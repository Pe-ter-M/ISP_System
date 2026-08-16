using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Payments.Core;
using InternetProvider.Api.Modules.Payments.Core.Models;
using InternetProvider.Api.Modules.Payments.Dtos;
using InternetProvider.Api.Modules.Payments.Services;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Payments.Core;

public static class PaymentsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/payments").WithTags("Payments");

        // ── GET: Available payment methods (registered gateways) ──
        group.MapGet("/methods", async (PaymentGatewayResolver resolver, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/payments/methods called");
            var methods = (await resolver.GetAvailableMethodsAsync())
                .Select(m => new PaymentMethodResponse(m.ToString(), LabelFor(m)))
                .ToList();
            return ApiResponse.Success(methods, $"Found {methods.Count} payment methods").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsCreate);

        // ── GET: Payment history (optionally scoped to a subscription) ──
        group.MapGet("/", async (
            int? subscriptionId,
            AppDbContext db,
            ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/payments called (subscriptionId: {SubscriptionId})", subscriptionId);

            var query = db.Payments.AsNoTracking().AsQueryable();
            if (subscriptionId.HasValue)
                query = query.Where(p => p.SubscriptionId == subscriptionId.Value);

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var response = payments.Select(p => new PaymentResponse(
                p.Id,
                p.SubscriptionId,
                p.AmountCents,
                p.Currency,
                p.PaymentMethod,
                p.Status,
                p.ReferenceNumber,
                p.PhoneNumber,
                p.ErrorMessage,
                p.CreatedAt,
                p.CompletedAt
            )).ToList();

            log.LogInformation("Returning {Count} payment records", response.Count);
            return ApiResponse.Success(response, $"Found {response.Count} payment records").ToResult();
        })
        .RequirePermission(Permissions.SubscriptionsView);
    }

    private static string LabelFor(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Mpesa => "M-Pesa",
            PaymentMethod.Airtel => "Airtel Money",
            PaymentMethod.Mock => "Mock (test)",
            _ => method.ToString()
        };
    }
}
