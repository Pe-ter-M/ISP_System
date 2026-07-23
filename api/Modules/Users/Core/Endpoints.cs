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

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            string? sortBy,
            bool? sortDesc,
            IUserService service,
            ILogger<LoggerMarker> log) =>
        {
            page ??= 1;
            pageSize ??= 10;
            bool desc = sortDesc ?? false;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            log.LogInformation("GET /api/users?page={Page}&size={Size}&search={Search}&sort={SortBy}&desc={SortDesc}",
                page, pageSize, search, sortBy, desc);

            var result = await service.GetAllAsync(page.Value, pageSize.Value, search, sortBy, desc);
            log.LogInformation("Returning {Count}/{Total} users", result.Items.Count, result.TotalCount);
            return ApiResponse.Success(result, "Users retrieved").ToResult();
        })
        .RequirePermission(Permissions.UsersView);

        group.MapGet("/{id:int}", async (int id, IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/users/{UserId} called", id);

            try
            {
                var user = await service.GetByIdAsync(id);
                return ApiResponse.Success(user, "User found").ToResult();
            }
            catch (NotFoundException)
            {
                return ApiResponse.Error("User not found", 404).ToResult();
            }
        })
        .RequirePermission(Permissions.UsersView);

        group.MapPost("/", async (CreateUserRequest req, IUserService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/users — creating user {Email}", req.Email);

            try
            {
                var user = await service.CreateAsync(req);
                log.LogInformation("Created user {UserId} — {Email}", user.Id, user.Email);
                return ApiResponse.Created(user, "User created successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.UsersCreate);
    }
}
