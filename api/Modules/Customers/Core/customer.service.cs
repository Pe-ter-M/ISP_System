using InternetProvider.Api.Modules.Customers.Dtos;
using InternetProvider.Api.Modules.Customers.Interfaces;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Customers.Core;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _repo;
    private readonly ILogger<CustomerService> _log;
    private readonly AppDbContext _db;

    public CustomerService(ICustomerRepository repo, ILogger<CustomerService> log, AppDbContext db)
    {
        _repo = repo;
        _log = log;
        _db = db;
    }

    public async Task<PaginatedResponse<CustomerSummaryResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc)
    {
        _log.LogDebug("Getting customers page {Page} size {PageSize}", page, pageSize);
        var result = await _repo.GetAllAsync(page, pageSize, search, sortBy, sortDesc);

        return new PaginatedResponse<CustomerSummaryResponse>
        {
            Items = result.Items.Select(x => MapSummary(x)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<CustomerDetailResponse> GetByIdAsync(int id)
    {
        _log.LogDebug("Getting customer detail for ID {CustomerId}", id);
        var result = await _repo.GetByIdAsync(id);

        if (result == null)
            throw new NotFoundException($"Customer with ID {id} not found");

        var subscriptions = await _repo.GetSubscriptionsAsync(id);
        var c = result.Customer;

        return new CustomerDetailResponse
        {
            Id = c.Id,
            UserId = c.UserId,
            CustomerCode = c.CustomerCode,
            FullName = result.FullName,
            BusinessName = c.BusinessName,
            CustomerType = c.CustomerType,
            Email = result.Email,
            Phone = c.Phone,
            ServiceAddress = c.ServiceAddress,
            City = c.City,
            Region = c.Region,
            GpsLat = c.GpsLat,
            GpsLng = c.GpsLng,
            Status = c.Status,
            Notes = c.Notes,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            Subscriptions = subscriptions,
        };
    }

    public async Task<CustomerSummaryResponse> CreateAsync(CreateCustomerRequest request)
    {
        _log.LogInformation("Creating customer: {FullName}", request.FullName);

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ConflictException("Email is required");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
            throw new ConflictException("Password must be at least 4 characters");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ConflictException("Full name is required");
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ConflictException("Phone number is required");
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException($"Email '{request.Email}' is already in use");
        if (await _repo.IsPhoneTakenAsync(request.Phone))
            throw new ConflictException($"Phone '{request.Phone}' is already in use");

        // Create user first
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            RoleId = 2, // Customer role
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Create customer record
        var code = await _repo.GenerateCustomerCodeAsync();
        var now = DateTime.UtcNow;
        var customer = new Models.Customer
        {
            UserId = user.Id,
            CustomerCode = code,
            BusinessName = request.BusinessName,
            CustomerType = request.CustomerType ?? "residential",
            Phone = request.Phone,
            ServiceAddress = request.ServiceAddress,
            City = request.City,
            Region = request.Region,
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now,
        };

        var created = await _repo.CreateAsync(customer);
        _log.LogInformation("Customer created: {Code} — {Name}", code, request.FullName);

        return new CustomerSummaryResponse
        {
            Id = created.Id,
            UserId = created.UserId,
            CustomerCode = created.CustomerCode,
            FullName = user.FullName,
            BusinessName = created.BusinessName,
            CustomerType = created.CustomerType,
            Email = user.Email,
            Phone = created.Phone,
            City = created.City,
            Region = created.Region,
            Status = created.Status,
            CreatedAt = created.CreatedAt,
        };
    }

    private static CustomerSummaryResponse MapSummary(CustomerWithUser x)
    {
        var c = x.Customer;
        return new CustomerSummaryResponse
        {
            Id = c.Id,
            UserId = c.UserId,
            CustomerCode = c.CustomerCode,
            FullName = x.FullName,
            BusinessName = c.BusinessName,
            CustomerType = c.CustomerType,
            Email = x.Email,
            Phone = c.Phone,
            City = c.City,
            Region = c.Region,
            Status = c.Status,
            CreatedAt = c.CreatedAt,
        };
    }
}
