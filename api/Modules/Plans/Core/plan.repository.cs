using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Plans.Core.Models;
using InternetProvider.Api.Modules.Plans.Interfaces;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Radius.Core.Models;

namespace InternetProvider.Api.Modules.Plans.Core;

public class PlanRepository : IPlanRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<PlanRepository> _log;

    public PlanRepository(AppDbContext db, ILogger<PlanRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<RadiusPackage>> GetAllActiveAsync()
    {
        _log.LogDebug("Fetching all active plans");
        var plans = await _db.RadiusPackages
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Id)
            .ToListAsync();
        _log.LogDebug("Found {Count} active plans", plans.Count);
        return plans;
    }

    public async Task<RadiusPackage?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching plan by ID {PlanId}", id);
        var plan = await _db.RadiusPackages.FindAsync(id);

        if (plan == null)
            _log.LogWarning("Plan with ID {PlanId} not found", id);
        else
            _log.LogDebug("Found plan {PlanId}: {Name}", id, plan.Name);

        return plan;
    }

    public async Task<RadiusPackage> CreateAsync(RadiusPackage plan)
    {
        _log.LogDebug("Creating plan {Name} with price {Price}", plan.Name, plan.PriceCents);
        _db.RadiusPackages.Add(plan);
        await _db.SaveChangesAsync();
        _log.LogDebug("Plan created with ID {PlanId}: {Name}", plan.Id, plan.Name);
        return plan;
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        var exists = await _db.RadiusPackages.AnyAsync(p => p.Name == name);
        _log.LogDebug("Plan name {Name} exists: {Exists}", name, exists);
        return exists;
    }

    public async Task<string?> GetGroupNameAsync(int groupId)
    {
        return await _db.RadiusGroups
            .Where(g => g.Id == groupId)
            .Select(g => g.GroupName)
            .FirstOrDefaultAsync();
    }

    public async Task SyncGroupPolicyAsync(RadiusPackage plan)
    {
        var groupName = await _db.RadiusGroups
            .Where(g => g.Id == plan.RadiusGroupId)
            .Select(g => g.GroupName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(groupName))
        {
            _log.LogWarning("Cannot sync policy: no RADIUS group found for ID {GroupId}", plan.RadiusGroupId);
            return;
        }

        _log.LogDebug("Syncing policy for group {Group} from plan {Plan}", groupName, plan.Name);

        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1. Delete existing replies of this group via EF Core (Cleanly tracks & maps database constraints)
        var existingReplies = await _db.Set<RadGroupReply>()
            .Where(r => r.GroupName == groupName)
            .ToListAsync();
        _db.Set<RadGroupReply>().RemoveRange(existingReplies);

        // 2. Prepare new reply values
        var repliesToInsert = new List<RadGroupReply>
        {
            new() { GroupName = groupName, Attribute = "Session-Timeout", Op = ":=", Value = plan.SessionTimeoutSeconds.ToString() },
            new() { GroupName = groupName, Attribute = "Idle-Timeout", Op = ":=", Value = plan.IdleTimeoutSeconds.ToString() }
        };

        if (plan.BandwidthDownKbps.HasValue)
        {
            repliesToInsert.Add(new() { GroupName = groupName, Attribute = "WISPr-Bandwidth-Max-Down", Op = ":=", Value = plan.BandwidthDownKbps.Value.ToString() });
        }
        if (plan.BandwidthUpKbps.HasValue)
        {
            repliesToInsert.Add(new() { GroupName = groupName, Attribute = "WISPr-Bandwidth-Max-Up", Op = ":=", Value = plan.BandwidthUpKbps.Value.ToString() });
        }

        await _db.Set<RadGroupReply>().AddRangeAsync(repliesToInsert);

        // 3. Delete existing checks of this group
        var existingChecks = await _db.Set<RadGroupCheck>()
            .Where(c => c.GroupName == groupName)
            .ToListAsync();
        _db.Set<RadGroupCheck>().RemoveRange(existingChecks);

        // 4. Prepare new check values
        var checksToInsert = new List<RadGroupCheck>
        {
            new() { GroupName = groupName, Attribute = "Simultaneous-Use", Op = ":=", Value = plan.MaxDevices.ToString() }
        };

        if (!plan.IsActive)
        {
            checksToInsert.Add(new() { GroupName = groupName, Attribute = "Auth-Type", Op = ":=", Value = "Reject" });
        }

        await _db.Set<RadGroupCheck>().AddRangeAsync(checksToInsert);

        // Persist all changes atomically through EF Core
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        _log.LogDebug("Policy sync complete for group {Group}", groupName);
    }

    // In the repository
    public async Task<RadiusPackage> UpdatePlanWithPolicyAsync(RadiusPackage plan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1. Fetch the OLD group ID from db before saving changes to detect modifications
        var oldGroupId = await _db.RadiusPackages
            .AsNoTracking()
            .Where(p => p.Id == plan.Id)
            .Select(p => p.RadiusGroupId)
            .FirstOrDefaultAsync();

        // 2. Update the plan row itself
        _db.RadiusPackages.Update(plan);
        await _db.SaveChangesAsync();

        // 3. Clean up the old group's attributes if the RADIUS group changed
        if (oldGroupId != 0 && oldGroupId != plan.RadiusGroupId)
        {
            var oldGroupName = await _db.RadiusGroups
                .Where(g => g.Id == oldGroupId)
                .Select(g => g.GroupName)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(oldGroupName))
            {
                _log.LogDebug("RadiusGroupId changed from {OldGroupId} to {NewGroupId}. Cleaning up old group {GroupName} policies.", oldGroupId, plan.RadiusGroupId, oldGroupName);

                var oldReplies = await _db.Set<RadGroupReply>()
                    .Where(r => r.GroupName == oldGroupName)
                    .ToListAsync();
                _db.Set<RadGroupReply>().RemoveRange(oldReplies);

                var oldChecks = await _db.Set<RadGroupCheck>()
                    .Where(c => c.GroupName == oldGroupName)
                    .ToListAsync();
                _db.Set<RadGroupCheck>().RemoveRange(oldChecks);
                
                await _db.SaveChangesAsync();
            }
        }

        // 4. Resolve new group name
        var groupName = await _db.RadiusGroups
            .Where(g => g.Id == plan.RadiusGroupId)
            .Select(g => g.GroupName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(groupName))
        {
            _log.LogWarning("Cannot sync policy: no RADIUS group found for ID {GroupId}", plan.RadiusGroupId);
            await tx.CommitAsync(); // plan update still stands even if group missing
            return plan;
        }

        // 5. radgroupreply (Sync rules to the new group)
        var existingReplies = await _db.Set<RadGroupReply>()
            .Where(r => r.GroupName == groupName)
            .ToListAsync();
        _db.Set<RadGroupReply>().RemoveRange(existingReplies);

        var repliesToInsert = new List<RadGroupReply>
        {
            new() { GroupName = groupName, Attribute = "Session-Timeout", Op = ":=", Value = plan.SessionTimeoutSeconds.ToString() },
            new() { GroupName = groupName, Attribute = "Idle-Timeout", Op = ":=", Value = plan.IdleTimeoutSeconds.ToString() }
        };

        if (plan.BandwidthDownKbps.HasValue)
        {
            repliesToInsert.Add(new() { GroupName = groupName, Attribute = "WISPr-Bandwidth-Max-Down", Op = ":=", Value = plan.BandwidthDownKbps.Value.ToString() });
        }
        if (plan.BandwidthUpKbps.HasValue)
        {
            repliesToInsert.Add(new() { GroupName = groupName, Attribute = "WISPr-Bandwidth-Max-Up", Op = ":=", Value = plan.BandwidthUpKbps.Value.ToString() });
        }

        await _db.Set<RadGroupReply>().AddRangeAsync(repliesToInsert);

        // 6. radgroupcheck
        var existingChecks = await _db.Set<RadGroupCheck>()
            .Where(c => c.GroupName == groupName)
            .ToListAsync();
        _db.Set<RadGroupCheck>().RemoveRange(existingChecks);

        var checksToInsert = new List<RadGroupCheck>
        {
            new() { GroupName = groupName, Attribute = "Simultaneous-Use", Op = ":=", Value = plan.MaxDevices.ToString() }
        };

        if (!plan.IsActive)
        {
            checksToInsert.Add(new() { GroupName = groupName, Attribute = "Auth-Type", Op = ":=", Value = "Reject" });
        }

        await _db.Set<RadGroupCheck>().AddRangeAsync(checksToInsert);

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        _log.LogDebug("Plan {PlanId} and policy for group {Group} updated atomically", plan.Id, groupName);
        return plan;
    }

    public async Task DeleteAsync(RadiusPackage plan)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1. Delete the plan row
        _db.RadiusPackages.Remove(plan);
        await _db.SaveChangesAsync();

        // 2. Resolve the RADIUS group name
        var groupName = await _db.RadiusGroups
            .Where(g => g.Id == plan.RadiusGroupId)
            .Select(g => g.GroupName)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(groupName))
        {
            _log.LogDebug("Plan deletion: syncing RADIUS policy removal for group {Group}", groupName);

            // 3. Delete group replies using strongly-typed entities
            var replies = await _db.Set<RadGroupReply>()
                .Where(r => r.GroupName == groupName)
                .ToListAsync();
            _db.Set<RadGroupReply>().RemoveRange(replies);

            // 4. Delete group checks using strongly-typed entities
            var checks = await _db.Set<RadGroupCheck>()
                .Where(c => c.GroupName == groupName)
                .ToListAsync();
            _db.Set<RadGroupCheck>().RemoveRange(checks);

            await _db.SaveChangesAsync();
        }
        else
        {
            _log.LogWarning("Plan deletion: no RADIUS group found for ID {GroupId} to clean up", plan.RadiusGroupId);
        }

        await tx.CommitAsync();
        _log.LogDebug("Plan {PlanId} and its associated RADIUS policy for group {Group} deleted atomically", plan.Id, groupName ?? "Unknown");
    }
}
