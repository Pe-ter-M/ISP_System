using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Users.Core;

public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", () => "Users endpoint - List");
        group.MapGet("/{id:int}", (int id) => $"Users endpoint - Get {id}");
        group.MapPost("/", () => "Users endpoint - Create");
        group.MapPut("/{id:int}", (int id) => $"Users endpoint - Update {id}");
        group.MapDelete("/{id:int}", (int id) => $"Users endpoint - Delete {id}");
    }
}
