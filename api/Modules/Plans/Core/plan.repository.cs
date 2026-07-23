using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Plans.Core.Models;
using InternetProvider.Api.Modules.Plans.Interfaces;
using InternetProvider.Api.Modules.Infrastructure.Core;

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
        _log.LogInformation("Creating plan {Name} with price {Price}", plan.Name, plan.PriceCents);
        _db.RadiusPackages.Add(plan);
        await _db.SaveChangesAsync();
        _log.LogInformation("Plan created with ID {PlanId}: {Name}", plan.Id, plan.Name);
        return plan;
    }

    public async Task UpdateAsync(RadiusPackage plan)
    {
        _log.LogDebug("Updating plan {PlanId}: {Name}", plan.Id, plan.Name);
        await _db.SaveChangesAsync();
        _log.LogInformation("Plan {PlanId} updated", plan.Id);
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

    public async Task SyncGroupQosAsync(RadiusPackage plan)
    {
        var groupName = await _db.RadiusGroups
            .Where(g => g.Id == plan.RadiusGroupId)
            .Select(g => g.GroupName)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(groupName))
        {
            _log.LogWarning("Cannot sync QoS: no RADIUS group found for ID {GroupId}", plan.RadiusGroupId);
            return;
        }

        _log.LogInformation("Syncing QoS for group {Group} from plan {Plan}", groupName, plan.Name);

        // Delete existing replies for this group
        await _db.Database.ExecuteSqlRawAsync(
            "DELETE FROM radgroupreply WHERE \"GroupName\" = {0}", groupName);

        // Insert Session-Timeout
        await _db.Database.ExecuteSqlRawAsync(
            "INSERT INTO radgroupreply (\"GroupName\", \"Attribute\", \"op\", \"Value\") VALUES ({0}, 'Session-Timeout', ':=', {1})",
            groupName, plan.SessionTimeoutSeconds.ToString());

        // Insert Idle-Timeout
        await _db.Database.ExecuteSqlRawAsync(
            "INSERT INTO radgroupreply (\"GroupName\", \"Attribute\", \"op\", \"Value\") VALUES ({0}, 'Idle-Timeout', ':=', {1})",
            groupName, plan.IdleTimeoutSeconds.ToString());

        // Insert bandwidth limits
        if (plan.BandwidthDownKbps.HasValue)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO radgroupreply (\"GroupName\", \"Attribute\", \"op\", \"Value\") VALUES ({0}, 'WISPr-Bandwidth-Max-Down', ':=', {1})",
                groupName, plan.BandwidthDownKbps.Value.ToString());
        }
        if (plan.BandwidthUpKbps.HasValue)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO radgroupreply (\"GroupName\", \"Attribute\", \"op\", \"Value\") VALUES ({0}, 'WISPr-Bandwidth-Max-Up', ':=', {1})",
                groupName, plan.BandwidthUpKbps.Value.ToString());
        }

        _log.LogInformation("QoS sync complete for group {Group}", groupName);
    }
}
