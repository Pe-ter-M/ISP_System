using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Users.Interfaces;
using InternetProvider.Api.Modules.Users.Dtos;
using InternetProvider.Api.Modules.Audit.Interfaces;

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
            var user = await service.GetByIdAsync(id);
            return ApiResponse.Success(user, "User found").ToResult();
        })
        .RequirePermission(Permissions.UsersView);

        group.MapPost("/", async (CreateUserRequest req, IUserService service, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/users — creating user {Email}", req.Email);

            try
            {
                var user = await service.CreateAsync(req);
                log.LogInformation("Created user {UserId} — {Email}", user.Id, user.Email);
                await audit.RecordAsync("user", user.Id, "create", $"User '{user.FullName}' ({user.Email}) created");
                return ApiResponse.Created(user, "User created successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.UsersCreate);

        group.MapPut("/{id:int}/permissions", async (int id, UpdateUserPermissionsRequest req, HttpContext ctx, IUserService service, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            var principal = ctx.Items["User"] as ClaimsPrincipal;
            var callerId = int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var cid) ? (int?)cid : null;

            log.LogInformation("PUT /api/users/{UserId}/permissions — {Count} overrides by caller {CallerId}", id, req.Overrides.Count, callerId);
            var result = await service.UpdatePermissionsAsync(id, req, callerId);
            await audit.RecordAsync("user", id, "update", $"Permissions for user #{id} updated ({req.Overrides.Count} overrides)");
            return ApiResponse.Success(result, "Permissions updated").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);
    }
}
