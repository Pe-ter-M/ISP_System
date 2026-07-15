using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Users.Core;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", () => "Users endpoint - List")
            .RequirePermission(Permissions.UsersView);

        group.MapGet("/{id:int}", (int id) => $"Users endpoint - Get {id}")
            .RequirePermission(Permissions.UsersView);

        group.MapPost("/", () => "Users endpoint - Create")
            .RequirePermission(Permissions.UsersCreate);

        group.MapPut("/{id:int}", (int id) => $"Users endpoint - Update {id}")
            .RequirePermission(Permissions.UsersUpdate);

        group.MapDelete("/{id:int}", (int id) => $"Users endpoint - Delete {id}")
            .RequirePermission(Permissions.UsersDelete);
    }
}
