using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Subscriptions.Dtos;
using InternetProvider.Api.Modules.Subscriptions.Interfaces;
using InternetProvider.Api.Modules.Subscriptions.Core.Models;
using InternetProvider.Api.Modules.Payments.Core.Models;
using InternetProvider.Api.Modules.Payments.Services;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Subscriptions.Core;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _repo;
    private readonly AppDbContext _db;
    private readonly PaymentGatewayResolver _paymentResolver;
    private readonly ILogger<SubscriptionService> _log;

    public SubscriptionService(
        ISubscriptionRepository repo,
        AppDbContext db,
        PaymentGatewayResolver paymentResolver,
        ILogger<SubscriptionService> log)
    {
        _repo = repo;
        _db = db;
        _paymentResolver = paymentResolver;
        _log = log;
    }

    public async Task<PaginatedResponse<SubscriptionResponse>> GetAllPagedAsync(
        int page = 1, int pageSize = 10, string? search = null, string? status = null,
        string? sortBy = null, bool sortDesc = false)
    {
        _log.LogDebug("Processing paged subscriptions request (page {Page}, size {PageSize}, search '{Search}', status '{Status}')", page, pageSize, search, status);

        var result = await _repo.GetAllPagedAsync(page, pageSize, search, status, sortBy, sortDesc);

        // Lazy expiry: any active subscription past its period end is marked expired and synced to RADIUS
        var responses = new List<SubscriptionResponse>(result.Items.Count);
        foreach (var s in result.Items)
        {
            await ExpireIfDueAsync(s);
            responses.Add(await BuildResponseAsync(s));
        }

        _log.LogDebug("Returning {Count}/{Total} subscriptions", responses.Count, result.TotalCount);
        return new PaginatedResponse<SubscriptionResponse>
        {
            Items = responses,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<SubscriptionStatsResponse> GetStatsAsync()
    {
        _log.LogDebug("Processing subscription stats request");
        return await _repo.GetStatsAsync();
    }

    public async Task<List<SubscriptionResponse>> GetByCustomerIdAsync(int customerId)
    {
        _log.LogDebug("Processing list subscriptions for customer ID {CustomerId}", customerId);
        var subscriptions = await _repo.GetByCustomerIdAsync(customerId);
        var responses = new List<SubscriptionResponse>();

        foreach (var s in subscriptions)
        {
            await ExpireIfDueAsync(s);
            responses.Add(await BuildResponseAsync(s));
        }

        return responses;
    }

    public async Task<SubscriptionResponse> GetByIdAsync(int id)
    {
        _log.LogDebug("Processing get subscription details for ID {Id}", id);
        var s = await _repo.GetByIdAsync(id);
        if (s == null)
        {
            _log.LogWarning("Subscription with ID {Id} not found", id);
            throw new NotFoundException($"Subscription with ID {id} not found");
        }

        // Lazy expiry: mark expired + sync RADIUS reject rules once the period end has passed
        await ExpireIfDueAsync(s);
        return await BuildResponseAsync(s);
    }

    public async Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, int? callerUserId = null)
    {
        if (request == null)
            throw new BadRequestException("Request body cannot be null. Please provide required subscription and billing attributes.");

        _log.LogInformation("Creating subscription service pipeline for customer {Id}", request.CustomerId);

        if (request.CustomerId <= 0)
            throw new BadRequestException("A valid positive CustomerId is required to start a subscription");
        if (request.PackageId <= 0)
            throw new BadRequestException("A valid positive PackageId is required to choose a speed plan");
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new BadRequestException("Mobile number is required for transaction STK requests");

        // 1. Validate customer exists and is active
        var customer = await _db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
            throw new NotFoundException($"Customer with ID {request.CustomerId} not found");
        if (customer.Status != "active")
            throw new ConflictException("Selected customer account status is designated inactive");

        // 1b. Ensure PPPoE credentials exist (legacy customers may have none — auto-generate like customer creation)
        if (string.IsNullOrWhiteSpace(customer.UsernamePpoe) || string.IsNullOrWhiteSpace(customer.PasswordPpoe))
        {
            _log.LogInformation("Customer {CustomerId} has no PPPoE credentials — auto-generating before subscription", customer.Id);
            customer.UsernamePpoe = $"{customer.CustomerCode.ToLower().Replace("-", "")}_ppoe";
            customer.PasswordPpoe = Guid.NewGuid().ToString()[..8].ToLower();
            customer.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        // 2. Validate package exists and is active
        var package = await _db.RadiusPackages.FindAsync(request.PackageId);
        if (package == null)
            throw new NotFoundException($"Package Plan with ID {request.PackageId} not found");
        if (!package.IsActive)
            throw new ConflictException("Selected package speed plan is currently inactive or suspended");

        // 2b. Only one active subscription per customer at a time
        if (await _repo.HasActiveSubscriptionForCustomerAsync(request.CustomerId))
            throw new ConflictException("This customer already has an active subscription. Suspend or expire it before creating a new one.");

        // 3. Process payment through chosen gateway
        var method = request.PaymentMethod;
        _log.LogInformation("Processing purchase of {Price} cents via payment provider: {Provider}", package.PriceCents, method);
        var gateway = await _paymentResolver.GetGatewayAsync(method);

        var paymentResult = await gateway.ProcessPaymentAsync(
            package.PriceCents, 
            request.PhoneNumber, 
            $"Sub-{customer.CustomerCode}"
        );

        if (!paymentResult.IsSuccess)
        {
            _log.LogWarning("Payment rejected by provider: {Reason}", paymentResult.ErrorMessage);
            throw new ConflictException($"Payment step failed and was rejected: {paymentResult.ErrorMessage}");
        }

        // 4. Instanstiate Subscription mapping details
        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            CustomerId = customer.Id,
            PackageId = package.Id,
            Username = customer.UsernamePpoe,
            Password = customer.PasswordPpoe,
            Status = "active",
            CurrentPeriodStart = now,
            CurrentPeriodEnd = now.AddDays(30), // standard 30 day package period duration
            AutoRenew = request.AutoRenew ?? false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = callerUserId
        };

        var payment = new Payment
        {
            AmountCents = package.PriceCents,
            Currency = "KES",
            PaymentMethod = method.ToString(), // Persisted string mapping (avoiding case sensitivity)
            Status = "Completed",
            ReferenceNumber = paymentResult.ReferenceNumber,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = now,
            CompletedAt = now
        };

        // 5. Submit to Repository for atomic execution
        var created = await _repo.CreateWithPaymentAsync(subscription, payment);

        return await BuildResponseAsync(created);
    }

    public async Task<SubscriptionResponse> CreateForCustomerUserAsync(int userId, CreateMySubscriptionRequest request)
    {
        _log.LogInformation("Processing customer self-subscription setup for logged-in User {UserId}", userId);

        if (request == null)
            throw new BadRequestException("Request payload cannot be empty.");

        // 1. Resolve customer profile from user account
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null)
        {
            _log.LogWarning("Customer profile missing for authenticated user {UserId}", userId);
            throw new NotFoundException("Our system was unable to find a Customer profile associated with your login account");
        }

        if (customer.Status != "active")
        {
            throw new ConflictException("Your customer account status is currently marked inactive. Cannot renew subscriptions.");
        }

        // 2. Wrap securely and delegating execution to the standard validation & creation pipeline
        var standardRequest = new CreateSubscriptionRequest(
            customer.Id,
            request.PackageId,
            request.AutoRenew,
            request.PaymentMethod,
            request.PhoneNumber,
            request.ReferenceNotes
        );

        return await CreateAsync(standardRequest, userId);
    }

    public async Task<SubscriptionResponse> UpdateAsync(int id, UpdateSubscriptionRequest request, int? callerUserId = null)
    {
        _log.LogInformation("Processing update subscription request for ID {Id}", id);

        var subscription = await _repo.GetByIdAsync(id);
        if (subscription == null)
            throw new NotFoundException($"Subscription with ID {id} not found");

        var oldUsername = subscription.Username;
        var oldPassword = subscription.Password;

        // Capture the original field values for change-history diffing
        var oldStatus = subscription.Status;
        var oldPeriodEnd = subscription.CurrentPeriodEnd;
        var oldAutoRenew = subscription.AutoRenew;
        var oldPackageId = subscription.PackageId;
        var oldPackageName = await GetPackageNameAsync(oldPackageId);

        var auditNotes = new List<string>();

        // Apply fields conditionally
        if (request.Status != null)
        {
            var status = request.Status.ToLower();
            if (status is not ("active" or "suspended" or "expired"))
                throw new ConflictException("Invalid subscription status choice. Choose 'active', 'suspended', or 'expired'.");
            subscription.Status = status;
            if (!string.Equals(oldStatus, status, StringComparison.OrdinalIgnoreCase))
                auditNotes.Add($"Status changed from '{oldStatus}' to '{status}'");
        }

        if (request.CurrentPeriodEnd.HasValue)
        {
            subscription.CurrentPeriodEnd = request.CurrentPeriodEnd.Value;
            // Only record when the billing period end actually changed (avoid
            // no-op entries when the edit form resubmits an unchanged value).
            if (request.CurrentPeriodEnd.Value != oldPeriodEnd)
                auditNotes.Add($"Billing period end changed from {oldPeriodEnd:yyyy-MM-dd} to {request.CurrentPeriodEnd.Value:yyyy-MM-dd}");
        }

        if (request.AutoRenew.HasValue)
        {
            subscription.AutoRenew = request.AutoRenew.Value;
                // Only record when auto-renew actually toggled.
            if (request.AutoRenew.Value != oldAutoRenew)
                auditNotes.Add($"Auto-renew {(request.AutoRenew.Value ? "enabled" : "disabled")}");
        }

        if (request.PackageId.HasValue && request.PackageId.Value != oldPackageId)
        {
            var targetPackage = await _db.RadiusPackages.FindAsync(request.PackageId.Value);
            if (targetPackage == null)
                throw new NotFoundException($"Package Plan with ID {request.PackageId.Value} not found");
            if (!targetPackage.IsActive)
                throw new ConflictException("Target package speed plan is currently set inactive");

            subscription.PackageId = targetPackage.Id;
            auditNotes.Add($"Plan changed from '{oldPackageName}' to '{targetPackage.Name}'");
        }

        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.UpdatedBy = callerUserId;

        // Persist updates and synchronize FreeRADIUS accounts
        await _repo.UpdateWithSyncAsync(subscription, oldUsername, oldPassword);

        // Record the change history (staff-only updates) for admin oversight
        if (auditNotes.Count > 0)
        {
            _db.SubscriptionAudits.Add(new SubscriptionAudit
            {
                SubscriptionId = subscription.Id,
                ChangedBy = callerUserId,
                Change = string.Join("; ", auditNotes),
                ChangedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            _log.LogInformation("Subscription {SubId} updated by user {UserId}: {Changes}", subscription.Id, callerUserId, string.Join("; ", auditNotes));
        }

        return await BuildResponseAsync(subscription);
    }

    private async Task<string> GetPackageNameAsync(int packageId)
    {
        var p = await _db.RadiusPackages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == packageId);
        return p?.Name ?? $"Package #{packageId}";
    }

    /// <summary>Mark a subscription expired (and sync RADIUS reject rules) once its period end has passed.</summary>
    private async Task ExpireIfDueAsync(Subscription s)
    {
        if (s.Status.Equals("active", StringComparison.OrdinalIgnoreCase) && s.CurrentPeriodEnd < DateTime.UtcNow)
        {
            _log.LogInformation("Subscription {SubId} for customer {CustomerId} is past period end — marking expired", s.Id, s.CustomerId);
            var oldUsername = s.Username;
            var oldPassword = s.Password;
            s.Status = "expired";
            s.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateWithSyncAsync(s, oldUsername, oldPassword);
        }
    }

    /// <summary>Build the enriched response (customer, plan, latest completed payment) for a subscription.</summary>
    private async Task<SubscriptionResponse> BuildResponseAsync(Subscription s)
    {
        var customer = await _db.Customers.AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == s.CustomerId);
        var plan = await _db.RadiusPackages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == s.PackageId);
        var payment = await _db.Payments.AsNoTracking()
            .Where(p => p.SubscriptionId == s.Id && p.Status == "Completed")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var response = new SubscriptionResponse(
            s.Id,
            s.CustomerId,
            s.PackageId,
            s.Username,
            s.Status,
            s.CurrentPeriodStart,
            s.CurrentPeriodEnd,
            s.AutoRenew,
            payment?.AmountCents ?? 0,
            payment?.ReferenceNumber,
            payment?.Status ?? "None",
            payment?.PaymentMethod,
            payment?.CompletedAt,
            customer?.User?.FullName ?? $"Customer #{s.CustomerId}",
            customer?.CustomerCode ?? "",
            plan?.Name ?? "Unknown plan"
        );

        // ── Audit attribution: who created the subscription ──
        response.CreatedByInfo = await ResolveActorAsync(s.CreatedBy);

        // ── Audit attribution: who last updated the subscription ──
        response.UpdatedByInfo = await ResolveActorAsync(s.UpdatedBy);
        response.UpdatedAt = s.UpdatedAt;

        // ── Change history: list of recorded updates (newest first) ──
        var audits = await _db.SubscriptionAudits.AsNoTracking()
            .Where(a => a.SubscriptionId == s.Id)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync();

        var history = new List<SubscriptionHistoryItem>(audits.Count);
        foreach (var a in audits)
        {
            history.Add(new SubscriptionHistoryItem(
                a.Id,
                await ResolveActorAsync(a.ChangedBy),
                a.Change,
                a.Notes,
                a.ChangedAt
            ));
        }
        response.AuditHistory = history;

        return response;
    }

    /// <summary>
    /// Resolve an actor (from a User.Id) into display info. If the user maps to a
    /// staff account we show their name/role/email/phone; otherwise it is recorded
    /// simply as a customer self-service action.
    /// </summary>
    private async Task<ActorInfo?> ResolveActorAsync(int? userId)
    {
        if (userId == null)
            return null;

        var staff = await _db.Staff.AsNoTracking()
            .Include(s => s.User).ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (staff?.User != null)
        {
            return new ActorInfo(
                "staff",
                staff.User.FullName,
                staff.User.Role?.Name,
                staff.User.Email,
                staff.User.Phone,
                staff.StaffCode
            );
        }

        // No staff record for this user — treat as a customer self-service action.
        return new ActorInfo("customer", null, null, null, null, null);
    }

    public async Task DeleteAsync(int id)
    {
        _log.LogInformation("Processing subscription hard delete request for ID {Id}", id);

        var subscription = await _repo.GetByIdAsync(id);
        if (subscription == null)
            throw new NotFoundException($"Subscription with ID {id} not found");

        await _repo.DeleteWithSyncAsync(subscription);
        _log.LogInformation("Subscription ID {Id} hard deleted successfully", id);
    }
}