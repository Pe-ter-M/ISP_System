using InternetProvider.Api.Modules.Nas.Dtos;

namespace InternetProvider.Api.Modules.Nas.Interfaces;

public interface INasService
{
    Task<PaginatedResponse<NasResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc);
    Task<NasResponse> GetByIdAsync(int id);
    Task<NasResponse> CreateAsync(CreateNasRequest request);
    Task<NasResponse> UpdateAsync(int id, UpdateNasRequest request);
    Task DeleteAsync(int id);
}
