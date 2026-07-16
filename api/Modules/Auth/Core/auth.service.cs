using InternetProvider.Api.Modules.Auth.Dtos;
using InternetProvider.Api.Modules.Auth.Interfaces;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Auth.Core;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly JwtService _jwt;

    public AuthService(IAuthRepository repo, JwtService jwt)
    {
        _repo = repo;
        _jwt = jwt;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _repo.FindActiveUserByEmailAsync(request.Email);
        if (user == null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        // ── Gather all permissions ──

        // 1. Get role's default permission IDs
        var rolePermIds = await _repo.GetRolePermissionIdsAsync(user.RoleId);

        // 2. Get all permissions and build a lookup
        var allPerms = await _repo.GetAllPermissionsAsync();
        var codeById = allPerms.ToDictionary(p => p.Id, p => p.Code);

        var roleCodes = rolePermIds
            .Select(id => codeById.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<string>()
            .ToHashSet();

        // 3. Get user overrides
        var userPerms = await _repo.GetUserPermissionsAsync(user.Id);

        var grantedIds = userPerms.Where(up => up.IsGranted).Select(up => up.PermissionId).ToHashSet();
        var deniedIds = userPerms.Where(up => !up.IsGranted).Select(up => up.PermissionId).ToHashSet();

        var grantedCodes = grantedIds
            .Select(id => codeById.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<string>();
        var deniedCodes = deniedIds
            .Select(id => codeById.GetValueOrDefault(id))
            .Where(c => c != null)
            .Cast<string>()
            .ToHashSet();

        // 4. Compute effective: (role defaults + extras) - denies
        var effective = roleCodes
            .Concat(grantedCodes)
            .Where(c => !deniedCodes.Contains(c))
            .Distinct()
            .ToList();

        // ── Generate token ──
        var token = _jwt.GenerateToken(
            user.Id, user.Email, user.FullName,
            user.RoleId, user.Role?.Name ?? "Unknown", effective
        );

        await _repo.UpdateLastLoginAsync(user.Id);

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
