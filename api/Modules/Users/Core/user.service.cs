using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Users.Dtos;
using InternetProvider.Api.Modules.Users.Interfaces;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Roles.Core.Models;
using InternetProvider.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InternetProvider.Api.Modules.Users.Core;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly ILogger<UserService> _log;
    private readonly AppDbContext _db;

    public UserService(IUserRepository repo, ILogger<UserService> log, AppDbContext db)
    {
        _repo = repo;
        _log = log;
        _db = db;
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

    public async Task<UserDetailResponse> GetByIdAsync(int id)
    {
        _log.LogDebug("Processing get user by ID {UserId}", id);
        var user = await _repo.GetByIdAsync(id)
            ?? throw new NotFoundException($"User not found");

        // Role default permissions
        var rolePermCodes = await _db.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
            .ToListAsync();

        // Per-user overrides
        var overrides = await _db.UserPermissions
            .Where(up => up.UserId == user.Id)
            .Join(_db.Permissions, up => up.PermissionId, p => p.Id,
                (up, p) => new UserPermissionOverride { Code = p.Code, IsGranted = up.IsGranted })
            .ToListAsync();

        // Apply overrides: is_granted=true adds, is_granted=false strips
        var effective = new HashSet<string>(rolePermCodes);
        foreach (var o in overrides)
        {
            if (o.IsGranted) effective.Add(o.Code);
            else effective.Remove(o.Code);
        }

        _log.LogInformation("Returning user {UserId}: {Email} with {PermCount} effective permissions", id, user.Email, effective.Count);
        return new UserDetailResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? "Unknown",
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Permissions = effective.Order().ToList(),
            PermissionOverrides = overrides,
        };
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

    public async Task<UserDetailResponse> UpdatePermissionsAsync(int userId, UpdateUserPermissionsRequest request, int? callerUserId)
    {
        var user = await _repo.GetByIdAsync(userId)
            ?? throw new NotFoundException($"User with ID {userId} not found");

        // Validate all submitted codes exist in the permissions table
        var codes = request.Overrides.Select(o => o.Code).Distinct().ToList();
        var foundPerms = await _db.Permissions
            .Where(p => codes.Contains(p.Code))
            .ToDictionaryAsync(p => p.Code, p => p.Id);

        var unknown = codes.Except(foundPerms.Keys).ToList();
        if (unknown.Count > 0)
            throw new BadRequestException($"Unknown permission codes: {string.Join(", ", unknown)}");

        // Compute the effective permission set (role defaults applied with overrides)
        // and enforce the view-first rule: every non-view permission needs its view.
        var roleDefaultCodes = await _db.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (_, p) => p.Code)
            .ToListAsync();

        var effective = roleDefaultCodes.ToHashSet();
        foreach (var o in request.Overrides)
        {
            if (o.IsGranted) effective.Add(o.Code);
            else effective.Remove(o.Code);
        }

        var dependencyError = Permissions.ValidateViewDependency(effective);
        if (dependencyError != null)
            throw new BadRequestException(dependencyError);

        // Replace all overrides atomically
        var existing = _db.UserPermissions.Where(up => up.UserId == userId);
        _db.UserPermissions.RemoveRange(existing);

        foreach (var o in request.Overrides)
        {
            _db.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = foundPerms[o.Code],
                IsGranted = o.IsGranted,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = callerUserId,
            });
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Permissions updated for user {UserId}: {Count} overrides by caller {CallerId}",
            userId, request.Overrides.Count, callerUserId);

        return await GetByIdAsync(userId);
    }
}
