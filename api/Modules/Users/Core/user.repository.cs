using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Users.Interfaces;
using InternetProvider.Api.Modules.Users.Dtos;
using InternetProvider.Api.Modules.Infrastructure.Core;

namespace InternetProvider.Api.Modules.Users.Core;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<UserRepository> _log;

    public UserRepository(AppDbContext db, ILogger<UserRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching user by ID {UserId}", id);
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null)
            _log.LogWarning("User with ID {UserId} not found", id);
        else
            _log.LogDebug("Found user {UserId}: {Email}", id, user.Email);

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        _log.LogDebug("Fetching user by email {Email}", email);
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
            _log.LogWarning("User with email {Email} not found", email);

        return user;
    }

    public async Task<PaginatedResponse<User>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false)
    {
        var query = _db.Users.Include(u => u.Role).AsQueryable();

        // ── Search ──
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                (u.Phone != null && u.Phone.Contains(term)));
        }

        // ── Sort ──
        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "email" => sortDesc ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "role" => sortDesc ? query.OrderByDescending(u => u.Role!.Name) : query.OrderBy(u => u.Role!.Name),
            "active" => sortDesc ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
            "created" => sortDesc ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            _ => query.OrderBy(u => u.Id) // default
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _log.LogDebug("Fetched {Count}/{Total} users (page {Page}, size {PageSize})", items.Count, totalCount, page, pageSize);
        return new PaginatedResponse<User>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<User> CreateAsync(User user)
    {
        _log.LogInformation("Creating user {Email} with role {RoleId}", user.Email, user.RoleId);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        _log.LogInformation("User created with ID {UserId}", user.Id);

        return (await GetByIdAsync(user.Id))!;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == email);
        _log.LogDebug("Email {Email} exists: {Exists}", email, exists);
        return exists;
    }
}
