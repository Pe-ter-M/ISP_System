using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Subscriptions.Interfaces;
using InternetProvider.Api.Modules.Subscriptions.Core.Models;
using InternetProvider.Api.Modules.Payments.Core.Models;
using InternetProvider.Api.Modules.Radius.Core.Models;

namespace InternetProvider.Api.Modules.Subscriptions.Core;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<SubscriptionRepository> _log;

    public SubscriptionRepository(AppDbContext db, ILogger<SubscriptionRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<Subscription>> GetAllAsync()
    {
        _log.LogDebug("Fetching all subscriptions from database");
        return await _db.Subscriptions
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Subscription>> GetByCustomerIdAsync(int customerId)
    {
        _log.LogDebug("Fetching all subscriptions for Customer ID {CustomerId}", customerId);
        return await _db.Subscriptions
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Subscription?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching subscription by ID {Id}", id);
        return await _db.Subscriptions.FindAsync(id);
    }

    public async Task<Subscription> CreateWithPaymentAsync(Subscription subscription, Payment payment)
    {
        _log.LogInformation("Creating subscription for Customer {CustomerId} via transactional commit", subscription.CustomerId);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Add and save subscription first to resolve generated primary key ID
            _db.Subscriptions.Add(subscription);
            await _db.SaveChangesAsync();

            // 2. Assign resolved subscription identifier to the payment log
            payment.SubscriptionId = subscription.Id;
            _db.Payments.Add(payment);

            // 3. Clear existing radcheck records for this specific username to prevent collisions
            var oldChecks = await _db.RadChecks
                .Where(r => r.UserName == subscription.Username)
                .ToListAsync();
            _db.RadChecks.RemoveRange(oldChecks);

            // 4. Add authentication credentials to radcheck table
            _db.RadChecks.Add(new RadCheck
            {
                UserName = subscription.Username,
                Attribute = "Cleartext-Password",
                Op = ":=",
                Value = subscription.Password
            });

            // 5. Clean historical mapping groups for this username
            var oldUserGroups = await _db.RadUserGroups
                .Where(g => g.UserName == subscription.Username)
                .ToListAsync();
            _db.RadUserGroups.RemoveRange(oldUserGroups);

            // 6. Find and bind the username to the Plan group name
            var groupName = await GetGroupNameByPackageIdAsync(subscription.PackageId);
            if (!string.IsNullOrEmpty(groupName))
            {
                _db.RadUserGroups.Add(new RadUserGroup
                {
                    UserName = subscription.Username,
                    GroupName = groupName,
                    Priority = 1
                });
            }
            else
            {
                _log.LogWarning("Package ID {PackageId} lacks an active associated Radius Group to bind rules", subscription.PackageId);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            _log.LogInformation("Subscription ID {SubId} and payments transaction processed atomically", subscription.Id);
            return subscription;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _log.LogError(ex, "Failed to complete subscription creation safely. Rolled back state.");
            throw;
        }
    }

    public async Task UpdateWithSyncAsync(Subscription subscription, string? oldUsername, string? oldPassword)
    {
        _log.LogInformation("Updating subscription ID {SubId} and syncing policies", subscription.Id);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.Subscriptions.Update(subscription);
            await _db.SaveChangesAsync();

            // Resolve modern and historical usernames for policy sweeping
            var currentUsername = subscription.Username;
            var currentPassword = subscription.Password;

            // 1. Cleans historical credentials if username changed
            if (!string.IsNullOrEmpty(oldUsername) && oldUsername != currentUsername)
            {
                var ancientChecks = await _db.RadChecks.Where(c => c.UserName == oldUsername).ToListAsync();
                _db.RadChecks.RemoveRange(ancientChecks);

                var ancientGroups = await _db.RadUserGroups.Where(g => g.UserName == oldUsername).ToListAsync();
                _db.RadUserGroups.RemoveRange(ancientGroups);
            }

            // 2. Synchronize current state to radcheck and radusergroup
            var activeChecks = await _db.RadChecks.Where(c => c.UserName == currentUsername).ToListAsync();
            _db.RadChecks.RemoveRange(activeChecks);

            var activeGroups = await _db.RadUserGroups.Where(g => g.UserName == currentUsername).ToListAsync();
            _db.RadUserGroups.RemoveRange(activeGroups);

            // Set rules up only if the subscription is physically active in billing cycle
            if (subscription.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            {
                _db.RadChecks.Add(new RadCheck
                {
                    UserName = currentUsername,
                    Attribute = "Cleartext-Password",
                    Op = ":=",
                    Value = currentPassword
                });

                var groupName = await GetGroupNameByPackageIdAsync(subscription.PackageId);
                if (!string.IsNullOrEmpty(groupName))
                {
                    _db.RadUserGroups.Add(new RadUserGroup
                    {
                        UserName = currentUsername,
                        GroupName = groupName,
                        Priority = 1
                    });
                }
            }
            else
            {
                // Subscription is inactive (suspended or expired) - we reject connection on FreeRADIUS side
                _db.RadChecks.Add(new RadCheck
                {
                    UserName = currentUsername,
                    Attribute = "Auth-Type",
                    Op = ":=",
                    Value = "Reject"
                });
                _log.LogInformation("Subscription {SubId} is inactive ({Status}). Reject rules added to freeRADIUS.", subscription.Id, subscription.Status);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            _log.LogInformation("Subscription ID {SubId} policies synced successfully", subscription.Id);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _log.LogError(ex, "Failed to complete subscription update safely. Rolled back state.");
            throw;
        }
    }

    public async Task DeleteWithSyncAsync(Subscription subscription)
    {
        _log.LogInformation("Deleting subscription {SubId} and washing FreeRADIUS residues", subscription.Id);

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // 1. Delete payment records and the subscription record itself
            var relatedPayments = await _db.Payments.Where(p => p.SubscriptionId == subscription.Id).ToListAsync();
            _db.Payments.RemoveRange(relatedPayments);

            _db.Subscriptions.Remove(subscription);
            await _db.SaveChangesAsync();

            // 2. Clean credentials and groups mapping on FreeRADIUS side
            var checks = await _db.RadChecks.Where(c => c.UserName == subscription.Username).ToListAsync();
            _db.RadChecks.RemoveRange(checks);

            var userGroups = await _db.RadUserGroups.Where(g => g.UserName == subscription.Username).ToListAsync();
            _db.RadUserGroups.RemoveRange(userGroups);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            _log.LogInformation("Atomic cascade delete complete for subscription {SubId}", subscription.Id);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            _log.LogError(ex, "Failed to delete subscription safely. Rolled back state.");
            throw;
        }
    }

    public async Task<string?> GetGroupNameByPackageIdAsync(int packageId)
    {
        return await (from p in _db.RadiusPackages
                      join g in _db.RadiusGroups on p.RadiusGroupId equals g.Id
                      where p.Id == packageId
                      select g.GroupName).FirstOrDefaultAsync();
    }

    public async Task<bool> HasActiveSubscriptionForCustomerAsync(int customerId)
    {
        return await _db.Subscriptions
            .AnyAsync(s => s.CustomerId == customerId && s.Status == "active");
    }
}