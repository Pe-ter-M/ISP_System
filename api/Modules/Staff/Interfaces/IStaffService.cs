using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Staff.Dtos;

namespace InternetProvider.Api.Modules.Staff.Interfaces;

public interface IStaffService
{
    Task<PaginatedResponse<StaffSummaryResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc);
    Task<StaffSummaryResponse> GetByIdAsync(int id);
    Task<StaffSummaryResponse> CreateAsync(CreateStaffRequest request);
    Task<StaffSummaryResponse> UpdateAsync(int id, UpdateStaffRequest request);
    Task<DeleteStaffResult> DeleteAsync(int id);
    Task<StaffStatsResponse> GetStatsAsync();
}
