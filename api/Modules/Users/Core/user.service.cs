using InternetProvider.Api.Modules.Users.Dtos;
using InternetProvider.Api.Modules.Users.Interfaces;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Users.Core;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly ILogger<UserService> _log;

    public UserService(IUserRepository repo, ILogger<UserService> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        _log.LogDebug("Processing get all users request");
        var users = await _repo.GetAllAsync();
        if (users.Count == 0)
        {
            _log.LogInformation("No users found in the database");
            throw new NotFoundException("No users found");
        }
        var responses = users.Select(MapToResponse).ToList();
        _log.LogDebug("Returning {Count} user records", responses.Count);
        return responses;
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        _log.LogDebug("Processing get user by ID {UserId}", id);
        var user = await _repo.GetByIdAsync(id);

        if (user == null)
        {
            throw new NotFoundException($"This user not found");
        }

        _log.LogInformation("Returning user {UserId}: {Email}", id, user.Email);
        return MapToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        _log.LogInformation("Processing create user request for email {Email} with role {RoleId}",
            request.Email, request.RoleId);

        if (await _repo.EmailExistsAsync(request.Email))
        {
            _log.LogWarning("Duplicate email attempt: {Email}", request.Email);
            throw new ConflictException($"Email '{request.Email}' is already in use.");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            RoleId = request.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(user);
        _log.LogInformation("User created successfully: {UserId} — {Email}", created.Id, created.Email);
        return MapToResponse(created);
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? "Unknown",
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
