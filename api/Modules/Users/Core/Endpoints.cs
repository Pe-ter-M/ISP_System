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

        group.MapGet("/", async (IUserService service) =>
        {
            var users = await service.GetAllAsync();
            return Results.Ok(users);
        });
       

        group.MapGet("/{id:int}", async (int id, IUserService service) =>
        {
            var user = await service.GetByIdAsync(id);
            return user == null ? Results.NotFound() : Results.Ok(user);
        })
        .RequirePermission(Permissions.UsersView);

        group.MapPost("/", async (CreateUserRequest req, IUserService service) =>
        {
            try
            {
                var user = await service.CreateAsync(req);
                return Results.Created($"/api/users/{user.Id}", user);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        })
        .RequirePermission(Permissions.UsersCreate);

    }
}
