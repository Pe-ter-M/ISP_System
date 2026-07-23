using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Users.Dtos;

namespace InternetProvider.Api.Modules.Users.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<PaginatedResponse<User>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false);
    Task<User> CreateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}
