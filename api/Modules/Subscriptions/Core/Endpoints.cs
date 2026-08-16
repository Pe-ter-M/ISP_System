using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Subscriptions.Interfaces;
using InternetProvider.Api.Modules.Subscriptions.Dtos;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Infrastructure.Core;

namespace InternetProvider.Api.Modules.Subscriptions.Core;

public static class SubscriptionEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        // ── GET: Paginated Subscriptions (Admin) with search/filter/sort ──
        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            string? status,
            string? sortBy,
            bool? sortDesc,
            ISubscriptionService service,
            ILogger<LoggerMarker> log) =>
        {
            page ??= 1;
            pageSize ??= 10;
            bool desc = sortDesc ?? false;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            log.LogInformation("GET /api/subscriptions?page={Page}&size={PageSize}&search={Search}&status={Status}&sort={SortBy}&desc={SortDesc}", page, pageSize, search, status, sortBy, desc);
            var result = await service.GetAllPagedAsync(page.Value, pageSize.Value, search, status, sortBy, desc);
            log.LogInformation("Returning {Count}/{Total} subscriptions", result.Items.Count, result.TotalCount);
            return ApiResponse.Success(result, $"Found {result.TotalCount} subscriptions").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsView);

        // ── GET: Subscription stats (Admin) ──
        group.MapGet("/stats", async (ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/subscriptions/stats called");
            var stats = await service.GetStatsAsync();
            return ApiResponse.Success(stats, "Subscription stats retrieved").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsView);

        // ── GET: Logged-in Customer fetches their own subscriptions ──
        // Reads the logged-in User's UserId from JWT Claims, resolves Customer, and fetches their subscriptions
        group.MapGet("/my", async (
            HttpContext context, 
            ISubscriptionService service, 
            AppDbContext db,
            ILogger<LoggerMarker> log) =>
        {
            var principal = context.Items["User"] as ClaimsPrincipal;
            if (principal == null)
                return Results.Unauthorized();

            // Extract the user ID from JWT claims
            var claimUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(claimUserId) || !int.TryParse(claimUserId, out var userId))
            {
                return Results.BadRequest(ApiResponse.Error("Malformed token claims payload").ToResult());
            }

            log.LogInformation("Customer GET /api/subscriptions/my for User ID {UserId}", userId);

            // Fetch the Customer ID associated with this User account
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                log.LogWarning("Subscriber context missing for authenticated user {UserId}", userId);
                return Results.NotFound(ApiResponse.Error("Customer profile record not found for this user account").ToResult());
            }

            var subscriptions = await service.GetByCustomerIdAsync(customer.Id);
            return ApiResponse.Success(subscriptions, "Your subscription details retrieved successfully").ToResult();
        });

        // ── GET: Individual Subscription By Id ──
        group.MapGet("/{id:int}", async (int id, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/subscriptions/{Id} called", id);
            var item = await service.GetByIdAsync(id);
            return ApiResponse.Success(item, "Subscription retrieved successfully").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsView);

        // ── POST: Logged-in Customer creates/pays their own subscription ──
        // Overrides or injects CustomerId securely from their JWT claims token to prevent spoofing
        group.MapPost("/my", async (
            CreateMySubscriptionRequest req, 
            HttpContext context, 
            ISubscriptionService service, 
            ILogger<LoggerMarker> log) =>
        {
            var principal = context.Items["User"] as ClaimsPrincipal;
            if (principal == null)
                return Results.Unauthorized();

            var claimUserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? principal.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(claimUserId) || !int.TryParse(claimUserId, out var userId))
            {
                return Results.BadRequest(ApiResponse.Error("Malformed token claims payload").ToResult());
            }

            log.LogInformation("Customer POST /api/subscriptions/my called for User ID {UserId}", userId);

            var item = await service.CreateForCustomerUserAsync(userId, req);
            return ApiResponse.Success(item, "Your subscription has been successfully purchased and set up").ToResult();
        });

        // ── POST: Dynamic Purchase and Setup Subscription ──
        group.MapPost("/", async (CreateSubscriptionRequest req, HttpContext context, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/subscriptions called - Customer ID: {Customer}", req.CustomerId);
            var callerUserId = GetCallerUserId(context);
            var item = await service.CreateAsync(req, callerUserId);
            return ApiResponse.Success(item, "Subscription successfully authenticated, paid, and connected").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsCreate);

        // ── PATCH: Modify Subscription Attributes (Partial updates) ──
        group.MapPatch("/{id:int}", async (int id, UpdateSubscriptionRequest req, HttpContext context, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PATCH /api/subscriptions/{Id} called", id);
            var callerUserId = GetCallerUserId(context);
            var item = await service.UpdateAsync(id, req, callerUserId);
            return ApiResponse.Success(item, "Subscription updated successfully and synchronized to FreeRADIUS").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsUpdate);

        // ── PATCH: Suspend / Resume a subscription (status-only change) ──
        // Governed by the dedicated subscription.suspend permission so a role can
        // suspend/resume without needing full subscription.update.
        group.MapPatch("/{id:int}/status", async (int id, UpdateSubscriptionRequest req, HttpContext context, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PATCH /api/subscriptions/{Id}/status called", id);
            var callerUserId = GetCallerUserId(context);
            var item = await service.UpdateAsync(id, req, callerUserId);
            return ApiResponse.Success(item, item.Status == "suspended"
                ? "Subscription suspended and synchronized to FreeRADIUS"
                : "Subscription resumed and synchronized to FreeRADIUS").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsSuspend);

        // ── DELETE: Hard Delete Subscription and flush RADIUS policies ──
        group.MapDelete("/{id:int}", async (int id, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/subscriptions/{Id} called", id);
            await service.DeleteAsync(id);
            return ApiResponse.Success(null, "Subscription policies and credentials successfully deleted from backend").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsDelete);
    }

    /// <summary>Extract the logged-in User.Id from the request's principal, or null if absent.</summary>
    private static int? GetCallerUserId(HttpContext context)
    {
        var principal = context.Items["User"] as ClaimsPrincipal;
        var claimUserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal?.FindFirst("sub")?.Value;
        return int.TryParse(claimUserId, out var userId) ? userId : null;
    }
}
