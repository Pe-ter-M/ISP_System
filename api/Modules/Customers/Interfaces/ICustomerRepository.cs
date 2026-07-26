using InternetProvider.Api.Modules.Customers.Core.Models;
using InternetProvider.Api.Modules.Customers.Dtos;

namespace InternetProvider.Api.Modules.Customers.Interfaces;

public interface ICustomerRepository
{
    Task<PaginatedResponse<CustomerWithUser>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc);
    Task<CustomerWithUser?> GetByIdAsync(int id);
    Task<List<CustomerSubscriptionDto>> GetSubscriptionsAsync(int customerId);
    Task<string> GenerateCustomerCodeAsync();
    Task<Customer> CreateAsync(Customer customer);
    Task<bool> IsPhoneTakenAsync(string phone);
}

public class CustomerWithUser
{
    public Customer Customer { get; set; } = null!;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
