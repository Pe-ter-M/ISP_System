using Microsoft.EntityFrameworkCore;
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

    public async Task<List<SubscriptionResponse>> GetAllAsync()
    {
        _log.LogDebug("Processing list all subscriptions request");
        var subscriptions = await _repo.GetAllAsync();
        var responses = new List<SubscriptionResponse>();

        foreach (var s in subscriptions)
        {
            // Resolve latest completed payment details for each subscription log
            var payment = await _db.Payments
                .Where(p => p.SubscriptionId == s.Id && p.Status == "Completed")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            responses.Add(new SubscriptionResponse(
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
                payment?.Status ?? "None"
            ));
        }

        _log.LogDebug("Fetched {Count} subscriptions", responses.Count);
        return responses;
    }

    public async Task<List<SubscriptionResponse>> GetByCustomerIdAsync(int customerId)
    {
        _log.LogDebug("Processing list subscriptions for customer ID {CustomerId}", customerId);
        var subscriptions = await _repo.GetByCustomerIdAsync(customerId);
        var responses = new List<SubscriptionResponse>();

        foreach (var s in subscriptions)
        {
            var payment = await _db.Payments
                .Where(p => p.SubscriptionId == s.Id && p.Status == "Completed")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            responses.Add(new SubscriptionResponse(
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
                payment?.Status ?? "None"
            ));
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

        var payment = await _db.Payments
            .Where(p => p.SubscriptionId == s.Id && p.Status == "Completed")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        return new SubscriptionResponse(
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
            payment?.Status ?? "None"
        );
    }

    public async Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request)
    {
        _log.LogInformation("Creating subscription service pipeline for customer {Id}", request.CustomerId);

        // 1. Validate customer exists and is active
        var customer = await _db.Customers.FindAsync(request.CustomerId);
        if (customer == null)
            throw new NotFoundException($"Customer with ID {request.CustomerId} not found");
        if (customer.Status != "active")
            throw new ConflictException("Selected customer account status is designated inactive");

        // 2. Validate package exists and is active
        var package = await _db.RadiusPackages.FindAsync(request.PackageId);
        if (package == null)
            throw new NotFoundException($"Package Plan with ID {request.PackageId} not found");
        if (!package.IsActive)
            throw new ConflictException("Selected package speed plan is currently inactive or suspended");

        // 3. Process payment through chosen gateway
        var method = request.PaymentMethod;
        _log.LogInformation("Processing purchase of {Price} cents via payment provider: {Provider}", package.PriceCents, method);
        var gateway = _paymentResolver.GetGateway(method);

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
            UpdatedAt = now
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

        return new SubscriptionResponse(
            created.Id,
            created.CustomerId,
            created.PackageId,
            created.Username,
            created.Status,
            created.CurrentPeriodStart,
            created.CurrentPeriodEnd,
            created.AutoRenew,
            payment.AmountCents,
            payment.ReferenceNumber,
            payment.Status
        );
    }

    public async Task<SubscriptionResponse> UpdateAsync(int id, UpdateSubscriptionRequest request)
    {
        _log.LogInformation("Processing update subscription request for ID {Id}", id);
        
        var subscription = await _repo.GetByIdAsync(id);
        if (subscription == null)
            throw new NotFoundException($"Subscription with ID {id} not found");

        var oldUsername = subscription.Username;
        var oldPassword = subscription.Password;

        // Apply fields conditionally
        if (request.Status != null)
        {
            var status = request.Status.ToLower();
            if (status is not ("active" or "suspended" or "expired"))
                throw new ConflictException("Invalid subscription status choice. Choose 'active', 'suspended', or 'expired'.");
            subscription.Status = status;
        }

        if (request.CurrentPeriodEnd.HasValue)
        {
            subscription.CurrentPeriodEnd = request.CurrentPeriodEnd.Value;
        }

        if (request.AutoRenew.HasValue)
        {
            subscription.AutoRenew = request.AutoRenew.Value;
        }

        if (request.PackageId.HasValue)
        {
            var targetPackage = await _db.RadiusPackages.FindAsync(request.PackageId.Value);
            if (targetPackage == null)
                throw new NotFoundException($"Package Plan with ID {request.PackageId.Value} not found");
            if (!targetPackage.IsActive)
                throw new ConflictException("Target package speed plan is currently set inactive");

            subscription.PackageId = targetPackage.Id;
        }

        subscription.UpdatedAt = DateTime.UtcNow;

        // Persist updates and synchronize FreeRADIUS accounts
        await _repo.UpdateWithSyncAsync(subscription, oldUsername, oldPassword);

        var payment = await _db.Payments
            .Where(p => p.SubscriptionId == subscription.Id && p.Status == "Completed")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        return new SubscriptionResponse(
            subscription.Id,
            subscription.CustomerId,
            subscription.PackageId,
            subscription.Username,
            subscription.Status,
            subscription.CurrentPeriodStart,
            subscription.CurrentPeriodEnd,
            subscription.AutoRenew,
            payment?.AmountCents ?? 0,
            payment?.ReferenceNumber,
            payment?.Status ?? "None"
        );
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