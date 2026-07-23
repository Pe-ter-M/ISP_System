using InternetProvider.Api.Modules.Users.Dtos;

namespace InternetProvider.Api.Modules.Users.Interfaces;

public interface IUserService
{
    Task<PaginatedResponse<UserResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc);
    Task<UserResponse?> GetByIdAsync(int id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
}
