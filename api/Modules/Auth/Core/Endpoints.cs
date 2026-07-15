using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using InternetProvider.Api.Modules.Auth.Dtos;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Auth.Core;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest req, AuthService auth) =>
        {
            var result = await auth.LoginAsync(req);
            if (result == null)
                return Results.Unauthorized();
            return Results.Ok(result);
        });

        group.MapGet("/me", (HttpContext http) =>
        {
            var user = http.Items["User"] as ClaimsPrincipal;
            if (user == null)
                return Results.Unauthorized();

            return Results.Ok(new
            {
                UserId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Email = user.FindFirstValue(ClaimTypes.Email),
                FullName = user.FindFirstValue(ClaimTypes.Name),
                Role = user.FindFirstValue("role_name"),
                Permissions = user.FindAll("permission").Select(c => c.Value).ToList()
            });
        }).RequirePermission(Permissions.UsersView);
    }
}
