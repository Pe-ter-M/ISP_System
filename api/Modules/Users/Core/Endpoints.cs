using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Users.Interfaces;
using InternetProvider.Api.Modules.Users.Dtos;

namespace InternetProvider.Api.Modules.Users.Core;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", async (IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/users called");
            var users = await service.GetAllAsync();
            log.LogInformation("Returning {Count} users", users.Count);
            return ApiResponse.Success(users, $"Found {users.Count} users").ToResult();
        })
        .RequirePermission(Permissions.UsersView);

        group.MapGet("/{id:int}", async (int id, IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/users/{UserId} called", id);
            var user = await service.GetByIdAsync(id);
            return ApiResponse.Success(user, "User found").ToResult();
        })
        .RequirePermission(Permissions.UsersView);

        group.MapPost("/", async (CreateUserRequest req, IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/users — creating user {Email}", req.Email);
            var user = await service.CreateAsync(req);
            log.LogInformation("Created user {UserId} — {Email}", user.Id, user.Email);
            return ApiResponse.Created(user, "User created successfully").ToResult();
        })
        .RequirePermission(Permissions.UsersCreate);
    }
}
