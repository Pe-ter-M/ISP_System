using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.RadAcct.Core;

public static class RadAcctEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/radius/sessions").WithTags("RADIUS Sessions");

        group.MapGet("/", () => "RADIUS Sessions endpoint - List active");
        group.MapGet("/{username}", (string username) => $"RADIUS Sessions endpoint - For user {username}");
    }
}
