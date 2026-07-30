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

    public async Task<List<PlanSummaryResponse>> GetAllAsync(bool includeSubscribersCount = false)
    {
        _log.LogDebug("Processing get all plans request (includeSubscribersCount: {IncludeCount})", includeSubscribersCount);
        var plans = await _repo.GetAllActiveAsync();
        
        var responses = new List<PlanSummaryResponse>();
        foreach (var p in plans)
        {
            int? count = null;
            if (includeSubscribersCount)
            {
                count = await _repo.GetActiveSubscribersCountAsync(p.Id);
            }

            responses.Add(new PlanSummaryResponse
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                PriceCents = p.PriceCents,
                BillingCycle = p.BillingCycle,
                BandwidthUpKbps = p.BandwidthUpKbps,
                BandwidthDownKbps = p.BandwidthDownKbps,
                MaxDevices = p.MaxDevices,
                ActiveSubscribersCount = count
            });
        }
        
        _log.LogDebug("Returning {Count} plan summaries", responses.Count);
        return responses;
    }

    public async Task<PlanDetailResponse> GetDetailByIdAsync(int id, bool includeSubscribersCount = false)
    {
        _log.LogDebug("Processing get plan detail for ID {PlanId} (includeSubscribersCount: {IncludeCount})", id, includeSubscribersCount);
        var plan = await _repo.GetByIdAsync(id);

        if (plan == null)
        {
            _log.LogWarning("Plan {PlanId} not found", id);
            throw new NotFoundException($"Plan with ID {id} not found");
        }

        var groupName = await _repo.GetGroupNameAsync(plan.RadiusGroupId) ?? "";
        
        int? count = null;
        if (includeSubscribersCount)
        {
            count = await _repo.GetActiveSubscribersCountAsync(plan.Id);
        }

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
            ActiveSubscribersCount = count
        };
    }

    public async Task<PlanSummaryResponse> CreateAsync(CreatePlanRequest request)
    {
        _log.LogDebug("Processing create plan request: {Name}", request.Name);

        // Server-side validation
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ConflictException("Plan name is required");
        if (request.PriceCents < 0)
            throw new ConflictException("Plan price cannot be negative");
        
        var maxDevices = request.MaxDevices ?? 1;
        if (maxDevices <= 0)
            throw new ConflictException("Max devices must be 1 or greater");

        if (request.SessionTimeoutSeconds is < 0)
            throw new ConflictException("Session timeout cannot be negative");
        if (request.IdleTimeoutSeconds is < 0)
            throw new ConflictException("Idle timeout cannot be negative");
        if (request.BandwidthUpKbps is < 0)
            throw new ConflictException("Upload bandwidth cannot be negative");
        if (request.BandwidthDownKbps is < 0)
            throw new ConflictException("Download bandwidth cannot be negative");

        if (await _repo.NameExistsAsync(request.Name))
        {
            _log.LogWarning("Duplicate plan name: {Name}", request.Name);
            throw new ConflictException($"A plan named '{request.Name}' already exists.");
        }

        var plan = new RadiusPackage
        {
            Name = request.Name,
            Description = request.Description,
            PriceCents = request.PriceCents,
            BillingCycle = request.BillingCycle ?? "monthly",
            BandwidthUpKbps = request.BandwidthUpKbps,
            BandwidthDownKbps = request.BandwidthDownKbps,
            SessionTimeoutSeconds = request.SessionTimeoutSeconds ?? 86400,
            IdleTimeoutSeconds = request.IdleTimeoutSeconds ?? 600,
            MaxDevices = maxDevices,
            IsActive = true,
            SortOrder = request.SortOrder ?? 0,
            CreatedAt = DateTime.UtcNow,
        };

        var created = await _repo.CreateAsync(plan);

        // Sync QoS to radgroupreply
        await SyncQos(created);

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
        _log.LogDebug("Processing update plan request for ID {PlanId}", id);

        var plan = await _repo.GetByIdAsync(id);
        if (plan == null)
        {
            _log.LogWarning("Plan {PlanId} not found for update", id);
            throw new NotFoundException($"Plan with ID {id} not found");
        }

        // Server-side validation
        if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
            throw new ConflictException("Plan name cannot be empty");
        if (request.PriceCents.HasValue && request.PriceCents.Value < 0)
            throw new ConflictException("Plan price cannot be negative");
        var maxDevices = request.MaxDevices ?? 1;
        if (maxDevices <= 0)
            throw new ConflictException("Max devices must be 1 or greater");

        if (request.SessionTimeoutSeconds.HasValue && request.SessionTimeoutSeconds.Value < 0)
            throw new ConflictException("Session timeout cannot be negative");
        if (request.IdleTimeoutSeconds.HasValue && request.IdleTimeoutSeconds.Value < 0)
            throw new ConflictException("Idle timeout cannot be negative");
        if (request.BandwidthUpKbps.HasValue && request.BandwidthUpKbps.Value < 0)
            throw new ConflictException("Upload bandwidth cannot be negative");
        if (request.BandwidthDownKbps.HasValue && request.BandwidthDownKbps.Value < 0)
            throw new ConflictException("Download bandwidth cannot be negative");

        if (request.Name != null && request.Name != plan.Name &&
            await _repo.NameExistsAsync(request.Name))
        {
            _log.LogWarning("Duplicate plan name on update: {Name}", request.Name);
            throw new ConflictException($"A plan named '{request.Name}' already exists.");
        }

        if (request.Name != null) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description;
        if (request.PriceCents.HasValue) plan.PriceCents = request.PriceCents.Value;
        if (request.BillingCycle != null) plan.BillingCycle = request.BillingCycle;
        if (request.BandwidthUpKbps.HasValue) plan.BandwidthUpKbps = request.BandwidthUpKbps;
        if (request.BandwidthDownKbps.HasValue) plan.BandwidthDownKbps = request.BandwidthDownKbps;
        if (request.SessionTimeoutSeconds.HasValue) plan.SessionTimeoutSeconds = request.SessionTimeoutSeconds.Value;
        if (request.IdleTimeoutSeconds.HasValue) plan.IdleTimeoutSeconds = request.IdleTimeoutSeconds.Value;
        plan.MaxDevices = maxDevices;
        if (request.SortOrder.HasValue) plan.SortOrder = request.SortOrder.Value;
        if (request.IsActive.HasValue) plan.IsActive = request.IsActive.Value;

        await _repo.UpdatePlanWithPolicyAsync(plan);

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
        _log.LogDebug("Processing hard-delete and RADIUS policy removal for plan ID {PlanId}", id);
        var plan = await _repo.GetByIdAsync(id);
        if (plan == null)
        {
            _log.LogWarning("Plan {PlanId} not found for delete", id);
            throw new NotFoundException($"Plan with ID {id} not found");
        }

        await _repo.DeleteAsync(plan);
    }

    private async Task SyncQos(RadiusPackage plan)
    {
        await _repo.SyncGroupPolicyAsync(plan);
    }
}
    