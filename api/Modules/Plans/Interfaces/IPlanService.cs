using InternetProvider.Api.Modules.Plans.Dtos;

namespace InternetProvider.Api.Modules.Plans.Interfaces;

public interface IPlanService
{
    Task<List<PlanSummaryResponse>> GetAllAsync(bool includeSubscribersCount = false);
    Task<PlanDetailResponse> GetDetailByIdAsync(int id, bool includeSubscribersCount = false);
    Task<PlanSummaryResponse> CreateAsync(CreatePlanRequest request);
    Task<PlanSummaryResponse> UpdateAsync(int id, UpdatePlanRequest request);
    Task DeleteAsync(int id);
}
