using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.RadPostAuth.Core;

public static class RadPostAuthEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/radius/auth-log").WithTags("RADIUS Auth Log");

        group.MapGet("/", () => "RADIUS Auth Log endpoint - List");
        group.MapGet("/{username}", (string username) => $"RADIUS Auth Log endpoint - For user {username}");
    }
}
