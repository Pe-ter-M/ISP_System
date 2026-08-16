using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Nas.Dtos;

namespace InternetProvider.Api.Modules.Nas.Interfaces;

public interface INasService
{
    Task<PaginatedResponse<NasResponse>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false, string? type = null);
    Task<NasResponse> GetByIdAsync(int id);
    Task<NasResponse> CreateAsync(CreateNasRequest request);
    Task<NasResponse> UpdateAsync(int id, UpdateNasRequest request);
    Task DeleteAsync(int id);
}
