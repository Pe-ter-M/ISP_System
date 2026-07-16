namespace InternetProvider.Api.Modules.Users.Dtos;

public record CreateUserRequest(
    string Email,
    string Password,
    string FullName,
    string? Phone,
    int RoleId
);
