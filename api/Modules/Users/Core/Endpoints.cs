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
            log.LogInformation("GET /api/users returning {Count} records with 200", users.Count);
            return ApiResponse.Success(users, $"Found {users.Count} users").ToResult();
        })
        .RequirePermission(Permissions.UsersView);

        group.MapGet("/{id:int}", async (int id, IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/users/{UserId} called", id);
            var user = await service.GetByIdAsync(id);

            if (user == null)
            {
                log.LogWarning("GET /api/users/{UserId} — not found", id);
                return ApiResponse.Error("User not found", 404).ToResult();
            }

            log.LogInformation("GET /api/users/{UserId} — found {Email}", id, user.Email);
            return ApiResponse.Success(user, "User found").ToResult();
        })
        .RequirePermission(Permissions.UsersView);

        group.MapPost("/", async (CreateUserRequest req, IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/users — creating user {Email}", req.Email);

            try
            {
                var user = await service.CreateAsync(req);
                log.LogInformation("POST /api/users — created {Email} with ID {UserId}", user.Email, user.Id);
                return ApiResponse.Created(user, "User created successfully").ToResult();
            }
            catch (InvalidOperationException ex)
            {
                log.LogWarning("POST /api/users — conflict: {Message}", ex.Message);
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.UsersCreate);
    }
}
