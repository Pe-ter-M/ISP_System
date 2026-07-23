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

    public async Task<PaginatedResponse<UserResponse>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false)
    {
        _log.LogDebug("Processing get users (page {Page}, size {PageSize})", page, pageSize);
        var result = await _repo.GetAllAsync(page, pageSize, search, sortBy, sortDesc);

        return new PaginatedResponse<UserResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<UserResponse?> GetByIdAsync(int id)
    {
        _log.LogDebug("Processing get user by ID {UserId}", id);
        var user = await _repo.GetByIdAsync(id);

        if (user == null)
        {
            throw new NotFoundException($"User not found");
        }

        _log.LogInformation("Returning user {UserId}: {Email}", id, user.Email);
        return MapToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        _log.LogInformation("Processing create user request for email {Email} with role {RoleId}",
            request.Email, request.RoleId);

        // Server-side validation
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ConflictException("Email is required");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
            throw new ConflictException("Password must be at least 4 characters");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ConflictException("Full name is required");

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
