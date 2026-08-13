using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Staff.Dtos;
using StaffEntity = InternetProvider.Api.Modules.Staff.Core.Models.Staff;

namespace InternetProvider.Api.Modules.Staff.Interfaces;

public interface IStaffRepository
{
    Task<PaginatedResponse<StaffEntity>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc);
    Task<StaffEntity?> GetByIdAsync(int id);
    Task<string> GenerateStaffCodeAsync();
    Task<StaffEntity> CreateAsync(StaffEntity staff);
    Task<StaffEntity> UpdateAsync(StaffEntity staff);
    Task<bool> IsPhoneTakenAsync(string phone);
    Task<StaffStatsResponse> GetStatsAsync();
}
