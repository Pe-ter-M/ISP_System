namespace InternetProvider.Api.Modules.Users.Dtos;

public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDetailResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Effective permissions after role defaults and per-user overrides are applied
    public List<string> Permissions { get; set; } = new();
    // Explicit per-user overrides so admin can see what was customised
    public List<UserPermissionOverride> PermissionOverrides { get; set; } = new();
}

public class UserPermissionOverride
{
    public string Code { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}

public record UpdateUserPermissionsRequest(List<PermissionOverrideInput> Overrides);
public record PermissionOverrideInput(string Code, bool IsGranted);
