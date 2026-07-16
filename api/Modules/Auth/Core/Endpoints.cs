using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using InternetProvider.Api.Modules.Auth.Dtos;
using InternetProvider.Api.Modules.Auth.Interfaces;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Auth.Core;

public static class AuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest req, IAuthService auth, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("Login attempt for {Email}", req.Email);
            var result = await auth.LoginAsync(req);

            if (result == null)
            {
                log.LogWarning("Login failed for {Email}", req.Email);
                return ApiResponse.Error("Invalid email or password", 401).ToResult();
            }

            log.LogInformation("Login successful for {Email} ({FullName})", req.Email, result.FullName);
            return ApiResponse.Success(result, "Login successful").ToResult();
        });

        group.MapGet("/me", (HttpContext http) =>
        {
            var user = http.Items["User"] as ClaimsPrincipal;
            if (user == null)
                return ApiResponse.Error("Not authenticated", 401).ToResult();

            var profile = new
            {
                UserId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!),
                Email = user.FindFirstValue(ClaimTypes.Email),
                FullName = user.FindFirstValue(ClaimTypes.Name),
                Role = user.FindFirstValue("role_name"),
                Permissions = user.FindAll("permission").Select(c => c.Value).ToList()
            };

            return ApiResponse.Success(profile, "Authenticated").ToResult();
        });
    }
}
