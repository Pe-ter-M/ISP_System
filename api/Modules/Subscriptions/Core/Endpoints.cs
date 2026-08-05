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

        // ── GET: All Active/Inactive Subscriptions (Admin only) ──
        group.MapGet("/", async (ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/subscriptions called");
            var items = await service.GetAllAsync();
            return ApiResponse.Success(items, $"Successfully retrieved {items.Count} subscriptions").ToResult();
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

        // ── POST: Dynamic Purchase and Setup Subscription ──
        group.MapPost("/", async (CreateSubscriptionRequest req, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/subscriptions called - Customer ID: {Customer}", req.CustomerId);
            var item = await service.CreateAsync(req);
            return ApiResponse.Success(item, "Subscription successfully authenticated, paid, and connected").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsCreate);

        // ── PUT: Modify Subscription Settings / Status / Package ──
        group.MapPut("/{id:int}", async (int id, UpdateSubscriptionRequest req, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/subscriptions/{Id} called", id);
            var item = await service.UpdateAsync(id, req);
            return ApiResponse.Success(item, "Subscription updated successfully and synchronized to FreeRADIUS").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsUpdate);

        // ── DELETE: Hard Delete Subscription and flush RADIUS policies ──
        group.MapDelete("/{id:int}", async (int id, ISubscriptionService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/subscriptions/{Id} called", id);
            await service.DeleteAsync(id);
            return ApiResponse.Success(null, "Subscription policies and credentials successfully deleted from backend").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsDelete);
    }
}
