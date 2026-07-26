using InternetProvider.Api.Modules.Nas.Core.Models;
using InternetProvider.Api.Modules.Nas.Dtos;

namespace InternetProvider.Api.Modules.Nas.Interfaces;

public interface INasRepository
{
    Task<NasClient?> GetByIdAsync(int id);
    Task<PaginatedResponse<NasClient>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false);
    Task<NasClient> CreateAsync(NasClient nasClient);
    Task<NasClient> UpdateAsync(NasClient nasClient);
    Task<bool> DeleteAsync(int id);
    Task<bool> NasnameExistsAsync(string nasname, int? excludeId = null);
}
