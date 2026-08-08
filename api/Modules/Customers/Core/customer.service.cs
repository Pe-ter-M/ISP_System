using InternetProvider.Api.Common;
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
            Items = result.Items.Select(MapSummary).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<CustomerDetailResponse> GetByIdAsync(int id)
    {
        _log.LogDebug("Getting customer detail for ID {CustomerId}", id);
        var c = await _repo.GetByIdAsync(id);

        if (c == null)
            throw new NotFoundException($"Customer with ID {id} not found");

        var subscriptions = await _repo.GetSubscriptionsAsync(id);

        return new CustomerDetailResponse
        {
            Id = c.Id,
            UserId = c.UserId,
            CustomerCode = c.CustomerCode,
            FullName = c.User!.FullName,
            BusinessName = c.BusinessName,
            CustomerType = c.CustomerType,
            Email = c.User!.Email,
            Phone = c.User!.Phone ?? string.Empty,
            ServiceAddress = c.ServiceAddress,
            City = c.City,
            Region = c.Region,
            GpsLat = c.GpsLat,
            GpsLng = c.GpsLng,
            UsernamePpoe = c.UsernamePpoe,
            PasswordPpoe = c.PasswordPpoe,
            Status = c.Status,
            Notes = c.Notes,
            CreatedAt = c.User!.CreatedAt,
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

        var customerRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Of(SystemRole.Customer))
            ?? throw new InvalidOperationException("Customer role not found — run database seeder first");

        // Create user first
        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            RoleId = customerRole.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Create customer record
        var code = await _repo.GenerateCustomerCodeAsync();
        var now = DateTime.UtcNow;

        // Auto-generate clean, unique PPPoE credentials
        // format: code_ppoe (e.g., phm0015_ppoe) and random 8 character secure password
        var generatedUsernamePPOE = $"{code.ToLower().Replace("-", "")}_ppoe";
        var generatedPasswordPPOE = Guid.NewGuid().ToString()[..8].ToLower();

        var customer = new Models.Customer
        {
            UserId = user.Id,
            CustomerCode = code,
            BusinessName = request.BusinessName,
            CustomerType = request.CustomerType ?? "residential",
            ServiceAddress = request.ServiceAddress,
            City = request.City,
            Region = request.Region,
            GpsLat = request.GpsLat,
            GpsLng = request.GpsLng,
            UsernamePpoe = generatedUsernamePPOE,
            PasswordPpoe = generatedPasswordPPOE,
            Status = "active",
            UpdatedAt = now,
        };

        var created = await _repo.CreateAsync(customer);
        _log.LogInformation("Customer created: {Code} — {Name} with auto-generated PPPoE credentials: {PpoeUser}", code, request.FullName, generatedUsernamePPOE);

        return new CustomerSummaryResponse
        {
            Id = created.Id,
            UserId = created.UserId,
            CustomerCode = created.CustomerCode,
            FullName = user.FullName,
            BusinessName = created.BusinessName,
            CustomerType = created.CustomerType,
            Email = user.Email,
            Phone = user.Phone ?? string.Empty,
            City = created.City,
            Region = created.Region,
            UsernamePpoe = created.UsernamePpoe,
            PasswordPpoe = created.PasswordPpoe,
            Status = created.Status,
            CreatedAt = user.CreatedAt,
        };
    }

    private static CustomerSummaryResponse MapSummary(Models.Customer c)
    {
        return new CustomerSummaryResponse
        {
            Id = c.Id,
            UserId = c.UserId,
            CustomerCode = c.CustomerCode,
            FullName = c.User!.FullName,
            BusinessName = c.BusinessName,
            CustomerType = c.CustomerType,
            Email = c.User!.Email,
            Phone = c.User!.Phone ?? string.Empty,
            City = c.City,
            Region = c.Region,
            UsernamePpoe = c.UsernamePpoe,
            PasswordPpoe = c.PasswordPpoe,
            Status = c.Status,
            CreatedAt = c.User!.CreatedAt,
        };
    }
}
