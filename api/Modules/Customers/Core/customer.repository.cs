using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Customers.Interfaces;
using InternetProvider.Api.Modules.Customers.Dtos;
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

    public async Task<PaginatedResponse<CustomerWithUser>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc)
    {
        var query = from c in _db.Customers
                    join u in _db.Users on c.UserId equals u.Id
                    select new CustomerWithUser
                    {
                        Customer = c,
                        FullName = u.FullName,
                        Email = u.Email,
                    };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(term) ||
                x.Customer.CustomerCode.ToLower().Contains(term) ||
                (x.Email != null && x.Email.ToLower().Contains(term)) ||
                x.Customer.Phone.Contains(term) ||
                (x.Customer.City != null && x.Customer.City.ToLower().Contains(term)));
        }

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDesc ? query.OrderByDescending(x => x.FullName) : query.OrderBy(x => x.FullName),
            "code" => sortDesc ? query.OrderByDescending(x => x.Customer.CustomerCode) : query.OrderBy(x => x.Customer.CustomerCode),
            "email" => sortDesc ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "city" => sortDesc ? query.OrderByDescending(x => x.Customer.City!) : query.OrderBy(x => x.Customer.City!),
            "region" => sortDesc ? query.OrderByDescending(x => x.Customer.Region!) : query.OrderBy(x => x.Customer.Region!),
            "status" => sortDesc ? query.OrderByDescending(x => x.Customer.Status) : query.OrderBy(x => x.Customer.Status),
            "created" => sortDesc ? query.OrderByDescending(x => x.Customer.CreatedAt) : query.OrderBy(x => x.Customer.CreatedAt),
            _ => query.OrderBy(x => x.Customer.Id)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        _log.LogDebug("Fetched {Count}/{Total} customers", items.Count, totalCount);
        return new PaginatedResponse<CustomerWithUser>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<CustomerWithUser?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching customer by ID {CustomerId}", id);
        var result = await (from c in _db.Customers
                            join u in _db.Users on c.UserId equals u.Id
                            where c.Id == id
                            select new CustomerWithUser
                            {
                                Customer = c,
                                FullName = u.FullName,
                                Email = u.Email,
                            }).FirstOrDefaultAsync();

        if (result == null)
            _log.LogWarning("Customer with ID {CustomerId} not found", id);

        return result;
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
        return await _db.Customers.AnyAsync(c => c.Phone == phone);
    }
}
