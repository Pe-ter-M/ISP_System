using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Customers.Core.Models;
using InternetProvider.Api.Modules.Customers.Dtos;

namespace InternetProvider.Api.Modules.Customers.Interfaces;

public interface ICustomerRepository
{
    Task<PaginatedResponse<Customer>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc, string? subscription = null);
    Task<Customer?> GetByIdAsync(int id);
    Task<List<CustomerSubscriptionDto>> GetSubscriptionsAsync(int customerId);
    Task<string> GenerateCustomerCodeAsync();
    Task<Customer> CreateAsync(Customer customer);
    Task<bool> IsPhoneTakenAsync(string phone);
}
