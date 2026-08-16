using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Customers.Dtos;

namespace InternetProvider.Api.Modules.Customers.Interfaces;

public interface ICustomerService
{
    Task<PaginatedResponse<CustomerSummaryResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc, string? subscription = null);
    Task<CustomerDetailResponse> GetByIdAsync(int id);
    Task<CustomerSummaryResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerSummaryResponse> UpdateAsync(int id, UpdateCustomerRequest request);
    Task<DeleteCustomerResult> DeleteAsync(int id);
}
