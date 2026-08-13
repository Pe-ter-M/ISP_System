using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Customers.Core.Models;
using InternetProvider.Api.Modules.Customers.Dtos;
using InternetProvider.Api.Modules.Customers.Interfaces;
using InternetProvider.Api.Modules.Infrastructure.Core;

namespace InternetProvider.Api.Modules.Customers.Core;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<CustomerRepository> _log;

    public CustomerRepository(AppDbContext db, ILogger<CustomerRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<PaginatedResponse<Customer>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc, string? subscription = null)
    {
        var query = _db.Customers.Include(c => c.User).AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.User!.FullName.ToLower().Contains(term) ||
                c.CustomerCode.ToLower().Contains(term) ||
                c.User!.Email.ToLower().Contains(term) ||
                (c.User!.Phone != null && c.User!.Phone.Contains(term)) ||
                (c.City != null && c.City.ToLower().Contains(term)));
        }

        // ── Subscription filter (all | none | active) ──
        if (!string.IsNullOrWhiteSpace(subscription))
        {
            var sub = subscription.ToLower();
            if (sub == "none")
                query = query.Where(c => !_db.Subscriptions.Any(s => s.CustomerId == c.Id && s.Status == "active"));
            else if (sub == "active")
                query = query.Where(c => _db.Subscriptions.Any(s => s.CustomerId == c.Id && s.Status == "active"));
        }

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDesc ? query.OrderByDescending(c => c.User!.FullName) : query.OrderBy(c => c.User!.FullName),
            "code" => sortDesc ? query.OrderByDescending(c => c.CustomerCode) : query.OrderBy(c => c.CustomerCode),
            "email" => sortDesc ? query.OrderByDescending(c => c.User!.Email) : query.OrderBy(c => c.User!.Email),
            "city" => sortDesc ? query.OrderByDescending(c => c.City!) : query.OrderBy(c => c.City!),
            "region" => sortDesc ? query.OrderByDescending(c => c.Region!) : query.OrderBy(c => c.Region!),
            "status" => sortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "created" => sortDesc ? query.OrderByDescending(c => c.User!.CreatedAt) : query.OrderBy(c => c.User!.CreatedAt),
            _ => query.OrderBy(c => c.Id)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        _log.LogDebug("Fetched {Count}/{Total} customers", items.Count, totalCount);
        return new PaginatedResponse<Customer>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching customer by ID {CustomerId}", id);
        var customer = await _db.Customers
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (customer == null)
            _log.LogWarning("Customer with ID {CustomerId} not found", id);

        return customer;
    }

    public async Task<List<CustomerSubscriptionDto>> GetSubscriptionsAsync(int customerId)
    {
        return await _db.Subscriptions
            .Where(s => s.CustomerId == customerId)
            .Join(_db.RadiusPackages, s => s.PackageId, p => p.Id,
                (s, p) => new CustomerSubscriptionDto
                {
                    Id = s.Id,
                    Username = s.Username,
                    PlanName = p.Name,
                    Status = s.Status,
                    CurrentPeriodEnd = s.CurrentPeriodEnd,
                })
            .ToListAsync();
    }

    public async Task<string> GenerateCustomerCodeAsync()
    {
        var last = await _db.Customers
            .OrderByDescending(c => c.Id)
            .Select(c => c.CustomerCode)
            .FirstOrDefaultAsync();

        int num = 1;
        if (last != null && last.StartsWith("PHM-"))
        {
            int.TryParse(last[4..], out num);
            num++;
        }
        return $"PHM-{num:D4}";
    }

    public async Task<Models.Customer> CreateAsync(Models.Customer customer)
    {
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        _log.LogInformation("Customer created with ID {CustomerId}: {Code}", customer.Id, customer.CustomerCode);
        return customer;
    }

    public async Task<bool> IsPhoneTakenAsync(string phone)
    {
        return await _db.Users.AnyAsync(u => u.Phone == phone);
    }
}
