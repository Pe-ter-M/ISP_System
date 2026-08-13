using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Plans.Dtos;

namespace InternetProvider.Api.Modules.Plans.Interfaces;

public interface IPlanService
{
    Task<List<PlanSummaryResponse>> GetAllAsync(bool includeSubscribersCount = false);
    Task<PaginatedResponse<PlanSummaryResponse>> GetAllAdminPagedAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? sortBy = null, bool sortDesc = false, bool includeSubscribersCount = true);
    Task<PlanStatsResponse> GetPlanStatsAsync();
    Task<PlanDetailResponse> GetDetailByIdAsync(int id, bool includeSubscribersCount = false);
    Task<PlanSummaryResponse> CreateAsync(CreatePlanRequest request);
    Task<PlanSummaryResponse> UpdateAsync(int id, UpdatePlanRequest request);
    Task DeleteAsync(int id);
}
