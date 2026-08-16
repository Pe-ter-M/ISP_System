using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Subscriptions.Dtos;

namespace InternetProvider.Api.Modules.Subscriptions.Interfaces;

public interface ISubscriptionService
{
    Task<PaginatedResponse<SubscriptionResponse>> GetAllPagedAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? sortBy = null, bool sortDesc = false);
    Task<SubscriptionStatsResponse> GetStatsAsync();
    Task<List<SubscriptionResponse>> GetByCustomerIdAsync(int customerId);
    Task<SubscriptionResponse> GetByIdAsync(int id);
    Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request, int? callerUserId = null);
    Task<SubscriptionResponse> CreateForCustomerUserAsync(int userId, CreateMySubscriptionRequest request);
    Task<SubscriptionResponse> UpdateAsync(int id, UpdateSubscriptionRequest request, int? callerUserId = null);
    Task DeleteAsync(int id);
}
