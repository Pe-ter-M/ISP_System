using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Roles.Core.Models;

namespace InternetProvider.Api.Modules.Auth.Interfaces;

public interface IAuthRepository
{
    Task<User?> FindActiveUserByEmailAsync(string email);
    Task<List<int>> GetRolePermissionIdsAsync(int roleId);
    Task<List<Permission>> GetAllPermissionsAsync();
    Task<List<UserPermission>> GetUserPermissionsAsync(int userId);
    Task UpdateLastLoginAsync(int userId);
}
