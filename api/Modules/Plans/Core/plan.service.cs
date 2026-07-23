using InternetProvider.Api.Modules.Plans.Dtos;
using InternetProvider.Api.Modules.Plans.Interfaces;
using InternetProvider.Api.Modules.Plans.Core.Models;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Plans.Core;

public class PlanService : IPlanService
{
    private readonly IPlanRepository _repo;
    private readonly ILogger<PlanService> _log;

    public PlanService(IPlanRepository repo, ILogger<PlanService> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<List<PlanSummaryResponse>> GetAllAsync()
    {
        _log.LogDebug("Processing get all plans request");
        var plans = await _repo.GetAllActiveAsync();
        var responses = plans.Select(p => new PlanSummaryResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            PriceCents = p.PriceCents,
            BillingCycle = p.BillingCycle,
            BandwidthUpKbps = p.BandwidthUpKbps,
            BandwidthDownKbps = p.BandwidthDownKbps,
            MaxDevices = p.MaxDevices,
        }).ToList();
        _log.LogDebug("Returning {Count} plan summaries", responses.Count);
        return responses;
    }

    public async Task<PlanDetailResponse> GetDetailByIdAsync(int id)
    {
        _log.LogDebug("Processing get plan detail for ID {PlanId}", id);
        var plan = await _repo.GetByIdAsync(id);

        if (plan == null)
        {
            _log.LogWarning("Plan {PlanId} not found", id);
            throw new NotFoundException($"Plan with ID {id} not found");
        }

        var groupName = await _repo.GetGroupNameAsync(plan.RadiusGroupId) ?? "";

        _log.LogInformation("Returning plan detail for {PlanId}: {Name}", id, plan.Name);
        return new PlanDetailResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PriceCents = plan.PriceCents,
            BillingCycle = plan.BillingCycle,
            BandwidthUpKbps = plan.BandwidthUpKbps,
            BandwidthDownKbps = plan.BandwidthDownKbps,
            SessionTimeoutSeconds = plan.SessionTimeoutSeconds,
            IdleTimeoutSeconds = plan.IdleTimeoutSeconds,
            MaxDevices = plan.MaxDevices,
            IsActive = plan.IsActive,
            SortOrder = plan.SortOrder,
            GroupName = groupName,
        };
    }

    public async Task<PlanSummaryResponse> CreateAsync(CreatePlanRequest request)
    {
        _log.LogInformation("Processing create plan request: {Name}", request.Name);

        if (await _repo.NameExistsAsync(request.Name))
        {
            _log.LogWarning("Duplicate plan name: {Name}", request.Name);
            throw new ConflictException($"A plan named '{request.Name}' already exists.");
        }

        var plan = new RadiusPackage
        {
            Name = request.Name,
            Description = request.Description,
            RadiusGroupId = request.RadiusGroupId,
            PriceCents = request.PriceCents,
            BillingCycle = request.BillingCycle ?? "monthly",
            BandwidthUpKbps = request.BandwidthUpKbps,
            BandwidthDownKbps = request.BandwidthDownKbps,
            SessionTimeoutSeconds = request.SessionTimeoutSeconds ?? 86400,
            IdleTimeoutSeconds = request.IdleTimeoutSeconds ?? 600,
            MaxDevices = request.MaxDevices ?? 1,
            IsActive = true,
            SortOrder = request.SortOrder ?? 0,
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _repo.CreateAsync(plan);

        // Sync QoS to radgroupreply
        await SyncQos(created);

        _log.LogInformation("Plan created successfully: {PlanId} — {Name}", created.Id, created.Name);
        return new PlanSummaryResponse
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description,
            PriceCents = created.PriceCents,
            BillingCycle = created.BillingCycle,
            BandwidthUpKbps = created.BandwidthUpKbps,
            BandwidthDownKbps = created.BandwidthDownKbps,
            MaxDevices = created.MaxDevices,
        };
    }

    public async Task<PlanSummaryResponse> UpdateAsync(int id, UpdatePlanRequest request)
    {
        _log.LogInformation("Processing update plan request for ID {PlanId}", id);

        var plan = await _repo.GetByIdAsync(id);
        if (plan == null)
        {
            _log.LogWarning("Plan {PlanId} not found for update", id);
            throw new NotFoundException($"Plan with ID {id} not found");
        }

        if (request.Name != null && request.Name != plan.Name &&
            await _repo.NameExistsAsync(request.Name))
        {
            _log.LogWarning("Duplicate plan name on update: {Name}", request.Name);
            throw new ConflictException($"A plan named '{request.Name}' already exists.");
        }

        if (request.Name != null) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description;
        if (request.RadiusGroupId.HasValue) plan.RadiusGroupId = request.RadiusGroupId.Value;
        if (request.PriceCents.HasValue) plan.PriceCents = request.PriceCents.Value;
        if (request.BillingCycle != null) plan.BillingCycle = request.BillingCycle;
        if (request.BandwidthUpKbps.HasValue) plan.BandwidthUpKbps = request.BandwidthUpKbps;
        if (request.BandwidthDownKbps.HasValue) plan.BandwidthDownKbps = request.BandwidthDownKbps;
        if (request.SessionTimeoutSeconds.HasValue) plan.SessionTimeoutSeconds = request.SessionTimeoutSeconds.Value;
        if (request.IdleTimeoutSeconds.HasValue) plan.IdleTimeoutSeconds = request.IdleTimeoutSeconds.Value;
        if (request.MaxDevices.HasValue) plan.MaxDevices = request.MaxDevices.Value;
        if (request.SortOrder.HasValue) plan.SortOrder = request.SortOrder.Value;

        await _repo.UpdateAsync(plan);

        // Re-sync QoS if relevant fields changed
        if (request.RadiusGroupId.HasValue || request.BandwidthUpKbps.HasValue ||
            request.BandwidthDownKbps.HasValue || request.SessionTimeoutSeconds.HasValue ||
            request.IdleTimeoutSeconds.HasValue)
        {
            await SyncQos(plan);
        }

        var groupName = await _repo.GetGroupNameAsync(plan.RadiusGroupId) ?? "";

        _log.LogInformation("Plan {PlanId} updated successfully", id);
        return new PlanSummaryResponse
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            PriceCents = plan.PriceCents,
            BillingCycle = plan.BillingCycle,
            BandwidthUpKbps = plan.BandwidthUpKbps,
            BandwidthDownKbps = plan.BandwidthDownKbps,
            MaxDevices = plan.MaxDevices,
        };
    }

    public async Task DeleteAsync(int id)
    {
        _log.LogInformation("Processing soft-delete for plan ID {PlanId}", id);
        var plan = await _repo.GetByIdAsync(id);
        if (plan == null)
        {
            _log.LogWarning("Plan {PlanId} not found for delete", id);
            throw new NotFoundException($"Plan with ID {id} not found");
        }

        plan.IsActive = false;
        await _repo.UpdateAsync(plan);
        _log.LogInformation("Plan {PlanId} deactivated", id);
    }

    private async Task SyncQos(RadiusPackage plan)
    {
        await _repo.SyncGroupQosAsync(plan);
    }
}
