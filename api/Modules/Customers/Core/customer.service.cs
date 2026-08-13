using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Customers.Dtos;
using InternetProvider.Api.Modules.Customers.Interfaces;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Roles.Core.Models;
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

    public async Task<PaginatedResponse<CustomerSummaryResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc, string? subscription = null)
    {
        _log.LogDebug("Getting customers page {Page} size {PageSize}", page, pageSize);
        var result = await _repo.GetAllAsync(page, pageSize, search, sortBy, sortDesc, subscription);

        // Resolve which of the returned customers currently hold an active subscription (one grouped query)
        var customerIds = result.Items.Select(c => c.Id).ToList();
        var withActiveSubscription = await _db.Subscriptions
            .Where(s => customerIds.Contains(s.CustomerId) && s.Status == "active")
            .Select(s => s.CustomerId)
            .Distinct()
            .ToListAsync();
        var activeSet = withActiveSubscription.ToHashSet();

        return new PaginatedResponse<CustomerSummaryResponse>
        {
            Items = result.Items.Select(c =>
            {
                var m = MapSummary(c);
                m.HasActiveSubscription = activeSet.Contains(c.Id);
                return m;
            }).ToList(),
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
        var hasActiveSubscription = await _db.Subscriptions.AnyAsync(s => s.CustomerId == id && s.Status == "active");

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
            HasActiveSubscription = hasActiveSubscription,
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

        await using var tx = await _db.Database.BeginTransactionAsync();

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
        await _db.SaveChangesAsync(); // flush to get user.Id; not committed yet

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
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(); // flush customer; not committed yet

        await tx.CommitAsync(); // both user and customer written atomically
        _log.LogInformation("Customer created: {Code} — {Name} with auto-generated PPPoE credentials: {PpoeUser}", code, request.FullName, generatedUsernamePPOE);

        return new CustomerSummaryResponse
        {
            Id = customer.Id,
            UserId = customer.UserId,
            CustomerCode = customer.CustomerCode,
            FullName = user.FullName,
            BusinessName = customer.BusinessName,
            CustomerType = customer.CustomerType,
            Email = user.Email,
            Phone = user.Phone ?? string.Empty,
            City = customer.City,
            Region = customer.Region,
            UsernamePpoe = customer.UsernamePpoe,
            PasswordPpoe = customer.PasswordPpoe,
            Status = customer.Status,
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
            GpsLat = c.GpsLat,
            GpsLng = c.GpsLng,
            UsernamePpoe = c.UsernamePpoe,
            PasswordPpoe = c.PasswordPpoe,
            Status = c.Status,
            CreatedAt = c.User!.CreatedAt,
        };
    }

    public async Task<CustomerSummaryResponse> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        _log.LogDebug("Processing update customer request for ID {CustomerId}", id);

        var customer = await _repo.GetByIdAsync(id);
        if (customer == null)
            throw new NotFoundException($"Customer with ID {id} not found");
        var user = customer.User;
        if (user == null)
            throw new NotFoundException($"User account for customer {id} not found");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ConflictException("Full name is required");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ConflictException("Email is required");
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ConflictException("Phone number is required");

        if (user.Email != request.Email && await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException($"Email '{request.Email}' is already in use");
        if (user.Phone != request.Phone && await _db.Users.AnyAsync(u => u.Phone == request.Phone))
            throw new ConflictException($"Phone '{request.Phone}' is already in use");

        var status = request.Status ?? customer.Status;
        if (status is not ("active" or "inactive"))
            throw new ConflictException("Invalid customer status choice. Choose 'active' or 'inactive'.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        customer.BusinessName = request.BusinessName;
        customer.CustomerType = string.IsNullOrWhiteSpace(request.CustomerType) ? "residential" : request.CustomerType;
        customer.ServiceAddress = request.ServiceAddress;
        customer.City = request.City;
        customer.Region = request.Region;
        customer.GpsLat = request.GpsLat;
        customer.GpsLng = request.GpsLng;
        customer.Status = status;
        customer.Notes = request.Notes;
        customer.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(customer);
        await _db.SaveChangesAsync(); // persist the tracked user changes too

        _log.LogInformation("Customer {CustomerId} updated successfully", id);
        return await BuildSummaryAsync(customer);
    }

    public async Task<DeleteCustomerResult> DeleteAsync(int id)
    {
        _log.LogDebug("Processing delete customer request for ID {CustomerId}", id);

        var customer = await _repo.GetByIdAsync(id);
        if (customer == null)
            throw new NotFoundException($"Customer with ID {id} not found");

        var hasSubscriptions = await _db.Subscriptions.AnyAsync(s => s.CustomerId == id);
        if (hasSubscriptions)
        {
            // Soft delete — keep billing and subscription history intact
            customer.Status = "inactive";
            customer.UpdatedAt = DateTime.UtcNow;
            if (customer.User != null)
            {
                customer.User.IsActive = false;
                customer.User.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
            _log.LogInformation("Customer {CustomerId} has subscription history — deactivated instead of deleted", id);
            return new DeleteCustomerResult(false, "Customer has subscription history — the account was deactivated instead of deleted.");
        }

        // Hard delete
        if (customer.User != null)
        {
            var overrides = await _db.UserPermissions.Where(up => up.UserId == customer.UserId).ToListAsync();
            _db.UserPermissions.RemoveRange(overrides);
        }
        _db.Customers.Remove(customer); // remove dependent first — User→Customer FK is required (no cascade)
        if (customer.User != null)
            _db.Users.Remove(customer.User);
        await _db.SaveChangesAsync();
        _log.LogInformation("Customer {CustomerId} and linked user account hard deleted", id);
        return new DeleteCustomerResult(true, "Customer deleted successfully.");
    }

    private async Task<CustomerSummaryResponse> BuildSummaryAsync(Models.Customer c)
    {
        var summary = MapSummary(c);
        summary.HasActiveSubscription = await _db.Subscriptions
            .AnyAsync(s => s.CustomerId == c.Id && s.Status == "active");
        return summary;
    }
}
