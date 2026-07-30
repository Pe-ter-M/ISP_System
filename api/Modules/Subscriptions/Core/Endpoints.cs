using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Infrastructure;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Subscriptions.Dtos;
using InternetProvider.Api.Modules.Subscriptions.Core.Models;
using InternetProvider.Api.Modules.Payments.Core.Models;
using InternetProvider.Api.Modules.Payments.Services;
using InternetProvider.Api.Modules.Radius.Core.Models;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Subscriptions.Core;

public static class SubscriptionEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        group.MapPost("/", async (
            CreateSubscriptionRequest req, 
            AppDbContext db, 
            PaymentGatewayResolver paymentResolver,
            ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("Processing POST /api/subscriptions for Customer ID {CustomerId}", req.CustomerId);

            // 1. Resolve Customer and fetch their auto-generated clean PPPoE credentials
            var customer = await db.Customers.FindAsync(req.CustomerId);
            if (customer == null)
            {
                log.LogWarning("Customer with ID {CustomerId} not found", req.CustomerId);
                return Results.BadRequest(ApiResponse.Error("Target Customer record not found").ToResult());
            }

            if (customer.Status != "active")
            {
                return Results.BadRequest(ApiResponse.Error("Customer is inactive and cannot create new subscriptions").ToResult());
            }

            // 2. Resolve target package plan details and its associated RadiusGroupId
            var package = await db.RadiusPackages.FindAsync(req.PackageId);
            if (package == null)
            {
                log.LogWarning("Package Plan with ID {PackageId} not found", req.PackageId);
                return Results.BadRequest(ApiResponse.Error("Selected Plan package not found").ToResult());
            }

            if (!package.IsActive)
            {
                return Results.BadRequest(ApiResponse.Error("Selected Plan package is currently suspended or inactive").ToResult());
            }

            // 3. Process subscription payment using the Dynamic Strategy pattern integration resolver
            log.LogInformation("Routing payment request of {Amount} cents through Strategy Gateway Provider: {Provider}", package.PriceCents, req.PaymentMethod);
            var gateway = paymentResolver.GetGateway(req.PaymentMethod);
            
            var paymentResult = await gateway.ProcessPaymentAsync(package.PriceCents, req.PhoneNumber, $"Sub-{customer.CustomerCode}");

            if (!paymentResult.IsSuccess)
            {
                log.LogError("Authorization failed: {Reason}", paymentResult.ErrorMessage);
                return Results.BadRequest(ApiResponse.Error($"Billing authorization failed: {paymentResult.ErrorMessage}").ToResult());
            }

            // 4. Perform atomic transactional insertion across our system and external FreeRADIUS lookup tables
            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Create the core system subscription entity using Customer PPPoE credentials
                var now = DateTime.UtcNow;
                var subscription = new Subscription
                {
                    CustomerId = customer.Id,
                    PackageId = package.Id,
                    Username = customer.UsernamePpoe,
                    Password = customer.PasswordPpoe,
                    Status = "active",
                    CurrentPeriodStart = now,
                    CurrentPeriodEnd = now.AddDays(30), // Default 30 day cycle duration
                    AutoRenew = req.AutoRenew ?? true,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Subscriptions.Add(subscription);
                await db.SaveChangesAsync(); // Saves to fetch generated subscription ID

                // Create the Payment Tracking receipt transaction row
                var payment = new Payment
                {
                    SubscriptionId = subscription.Id,
                    AmountCents = package.PriceCents,
                    Currency = "KES",
                    PaymentMethod = req.PaymentMethod,
                    Status = "Completed",
                    ReferenceNumber = paymentResult.ReferenceNumber,
                    PhoneNumber = req.PhoneNumber,
                    CreatedAt = now,
                    CompletedAt = now
                };
                db.Payments.Add(payment);

                // ── POPULATE FREERADIUS CONTROLS AND CREDENTIAL TABLES ──

                // A. Delete existing radcheck auth records for this specific customer username to prevent duplicates
                var oldRadChecks = await db.RadChecks
                    .Where(rc => rc.UserName == customer.UsernamePpoe)
                    .ToListAsync();
                db.RadChecks.RemoveRange(oldRadChecks);

                // Insert into radcheck table mapping the cleartext PPPoE password
                db.RadChecks.Add(new RadCheck
                {
                    UserName = customer.UsernamePpoe,
                    Attribute = "Cleartext-Password",
                    Op = ":=",
                    Value = customer.PasswordPpoe
                });

                // B. Resolve the RADIUS dynamic speed mapping group name
                var groupName = await db.RadiusGroups
                    .Where(g => g.Id == package.RadiusGroupId)
                    .Select(g => g.GroupName)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(groupName))
                {
                    // Clean up any historical mapped usergroup associations for this username
                    var oldUserGroups = await db.RadUserGroups
                        .Where(rug => rug.UserName == customer.UsernamePpoe)
                        .ToListAsync();
                    db.RadUserGroups.RemoveRange(oldUserGroups);

                    // Insert into radusergroup to bind customer PPPoE username to bandwidth rules
                    db.RadUserGroups.Add(new RadUserGroup
                    {
                        UserName = customer.UsernamePpoe,
                        GroupName = groupName,
                        Priority = 1
                    });
                }
                else
                {
                    log.LogWarning("Assigned package '{Package}' lacks an active matching Radius Group row to sync policies properly", package.Name);
                }

                await db.SaveChangesAsync();
                await tx.CommitAsync();

                log.LogInformation("Subscription transaction successfully completed and written to FreeRADIUS for user {PpoeUser}", customer.UsernamePpoe);

                var response = new SubscriptionResponse(
                    subscription.Id,
                    subscription.CustomerId,
                    subscription.PackageId,
                    subscription.Username,
                    subscription.Status,
                    subscription.CurrentPeriodStart,
                    subscription.CurrentPeriodEnd,
                    subscription.AutoRenew,
                    payment.AmountCents,
                    payment.ReferenceNumber,
                    payment.Status
                );

                return Results.Ok(ApiResponse.Success(response, "Subscription successfully completed").ToResult());
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                log.LogError(ex, "Failed to complete subscription transaction safely. Rolled back state.");
                throw;
            }
        });

        group.MapGet("/", () => "Subscriptions endpoint - List");
        group.MapGet("/{id:int}", (int id) => $"Subscriptions endpoint - Get {id}");
        group.MapPut("/{id:int}", (int id) => $"Subscriptions endpoint - Update {id}");
        group.MapDelete("/{id:int}", (int id) => $"Subscriptions endpoint - Delete {id}");
    }
}
