using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Users.Interfaces;
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

    public async Task<List<User>> GetAllAsync()
    {
        _log.LogDebug("Fetching all users from database");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var users = await _db.Users
            .Include(u => u.Role)
            .OrderBy(u => u.Id)
            .ToListAsync();

        sw.Stop();
        _log.LogDebug("Retrieved {Count} users from database in {ElapsedMs}ms", users.Count, sw.ElapsedMilliseconds);
        return users;
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
