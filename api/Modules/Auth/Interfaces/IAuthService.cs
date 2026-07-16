using InternetProvider.Api.Modules.Auth.Dtos;

namespace InternetProvider.Api.Modules.Auth.Interfaces;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
