using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Subscriptions.Core.Models;
using InternetProvider.Api.Modules.Subscriptions.Dtos;
using InternetProvider.Api.Modules.Payments.Core.Models;

namespace InternetProvider.Api.Modules.Subscriptions.Interfaces;

public interface ISubscriptionRepository
{
    Task<PaginatedResponse<Subscription>> GetAllPagedAsync(int page = 1, int pageSize = 10, string? search = null, string? status = null, string? sortBy = null, bool sortDesc = false);
    Task<SubscriptionStatsResponse> GetStatsAsync();
    Task<List<Subscription>> GetByCustomerIdAsync(int customerId);
    Task<Subscription?> GetByIdAsync(int id);
    Task<Subscription> CreateWithPaymentAsync(Subscription subscription, Payment payment);
    Task UpdateWithSyncAsync(Subscription subscription, string? oldUsername, string? oldPassword);
    Task DeleteWithSyncAsync(Subscription subscription);
    Task<string?> GetGroupNameByPackageIdAsync(int packageId);
    Task<bool> HasActiveSubscriptionForCustomerAsync(int customerId);
}