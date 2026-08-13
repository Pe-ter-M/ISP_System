using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Plans.Core.Models;
using InternetProvider.Api.Modules.Plans.Dtos;

namespace InternetProvider.Api.Modules.Plans.Interfaces;

public interface IPlanRepository
{
    Task<List<RadiusPackage>> GetAllActiveAsync();
    Task<PaginatedResponse<RadiusPackage>> GetAllPagedAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? sortBy = null, bool sortDesc = false);
    Task<List<RadiusPackage>> GetFilteredAsync(string? search = null, string? status = null);
    Task<PlanStatsResponse> GetStatsAsync();
    Task<RadiusPackage?> GetByIdAsync(int id);
    Task<RadiusPackage> CreateAsync(RadiusPackage plan);
    Task<bool> NameExistsAsync(string name);
    Task<string?> GetGroupNameAsync(int groupId);
    Task SyncGroupPolicyAsync(RadiusPackage plan);
    Task<RadiusPackage> UpdatePlanWithPolicyAsync(RadiusPackage plan);
    Task DeleteAsync(RadiusPackage plan);
    Task<int> GetActiveSubscribersCountAsync(int planId);
}
