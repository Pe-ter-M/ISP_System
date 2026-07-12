using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Settings.Core;

public static class SettingsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        group.MapGet("/", () => "Settings endpoint - List");
        group.MapGet("/{key}", (string key) => $"Settings endpoint - Get {key}");
        group.MapPut("/{key}", (string key) => $"Settings endpoint - Update {key}");
    }
}
