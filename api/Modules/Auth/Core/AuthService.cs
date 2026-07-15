using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Auth.Dtos;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Auth.Core;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthService(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // ── Gather all permissions in one efficient query ──

        // 1. Get IDs of permissions the role grants by default
        var rolePermIds = await _db.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        // 2. Get the actual code strings for those IDs
        var allPerms = await _db.Permissions.ToListAsync();
        var codeById = allPerms.ToDictionary(p => p.Id, p => p.Code);

        var roleCodes = rolePermIds.Select(id => codeById.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<string>()
            .ToHashSet();

        // 3. Get user overrides
        var userPerms = await _db.UserPermissions
            .Where(up => up.UserId == user.Id)
            .ToListAsync();

        var grantedIds = userPerms.Where(up => up.IsGranted).Select(up => up.PermissionId).ToHashSet();
        var deniedIds = userPerms.Where(up => !up.IsGranted).Select(up => up.PermissionId).ToHashSet();

        var grantedCodes = grantedIds.Select(id => codeById.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<string>();
        var deniedCodes = deniedIds.Select(id => codeById.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<string>()
            .ToHashSet();

        // 4. Compute effective: (role defaults + extras) - denies
        var effective = roleCodes.Concat(grantedCodes)
            .Where(c => !deniedCodes.Contains(c))
            .Distinct()
            .ToList();

        // ── Generate token ──
        var token = _jwt.GenerateToken(
            user.Id,
            user.Email,
            user.FullName,
            user.RoleId,
            user.Role?.Name ?? "Unknown",
            effective
        );

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role?.Name ?? "Unknown",
            Permissions = effective
        };
    }
}
