using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace InternetProvider.Api.Services;

public static class PermissionAuthorization
{
    public static IApplicationBuilder UseJwtAuth(this WebApplication app)
    {
        return app.Use(async (context, next) =>
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader["Bearer ".Length..];
                var jwtService = context.RequestServices.GetRequiredService<JwtService>();
                var principal = jwtService.ValidateToken(token);

                if (principal != null)
                    context.Items["User"] = principal;
            }

            await next();
        });
    }

    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder builder, string permission)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var principal = context.HttpContext.Items["User"] as ClaimsPrincipal;

            if (principal == null)
                return Results.Unauthorized();

            var permissions = principal.FindAll("permission").Select(c => c.Value);

            if (!permissions.Contains(permission))
                return Results.Forbid();

            return await next(context);
        });
    }
}
