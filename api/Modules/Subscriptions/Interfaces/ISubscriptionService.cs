using InternetProvider.Api.Modules.Subscriptions.Dtos;

namespace InternetProvider.Api.Modules.Subscriptions.Interfaces;

public interface ISubscriptionService
{
    Task<List<SubscriptionResponse>> GetAllAsync();
    Task<List<SubscriptionResponse>> GetByCustomerIdAsync(int customerId);
    Task<SubscriptionResponse> GetByIdAsync(int id);
    Task<SubscriptionResponse> CreateAsync(CreateSubscriptionRequest request);
    Task<SubscriptionResponse> CreateForCustomerUserAsync(int userId, CreateMySubscriptionRequest request);
    Task<SubscriptionResponse> UpdateAsync(int id, UpdateSubscriptionRequest request);
    Task DeleteAsync(int id);
}