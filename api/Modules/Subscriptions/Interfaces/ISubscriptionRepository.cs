using InternetProvider.Api.Modules.Subscriptions.Core.Models;
using InternetProvider.Api.Modules.Payments.Core.Models;

namespace InternetProvider.Api.Modules.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task<List<Subscription>> GetAllAsync();
    Task<List<Subscription>> GetByCustomerIdAsync(int customerId);
    Task<Subscription?> GetByIdAsync(int id);
    Task<Subscription> CreateWithPaymentAsync(Subscription subscription, Payment payment);
    Task UpdateWithSyncAsync(Subscription subscription, string? oldUsername, string? oldPassword);
    Task DeleteWithSyncAsync(Subscription subscription);
    Task<string?> GetGroupNameByPackageIdAsync(int packageId);
    Task<bool> HasActiveSubscriptionForCustomerAsync(int customerId);
}