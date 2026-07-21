using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Settings.Interfaces;
using InternetProvider.Api.Modules.Settings.Dtos;

namespace InternetProvider.Api.Modules.Settings.Core;

public static class SettingsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/settings").WithTags("Settings");

        // GET /api/settings/{key} — public, no auth
        group.MapGet("/{key}", async (string key, ISettingService service, ILogger<LoggerMarker> log) =>
        {
            log.LogDebug("GET /api/settings/{Key} called", key);
            var setting = await service.GetByKeyAsync(key);
            if (setting == null)
                return ApiResponse.Error($"Setting '{key}' not found", 404).ToResult();
            return ApiResponse.Success(setting, "OK").ToResult();
        });

        // POST /api/settings — create, requires settings.update
        group.MapPost("/", async (CreateSettingRequest req, ISettingService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/settings — creating {Key}", req.Key);
            var setting = await service.CreateAsync(req, null);
            log.LogInformation("Setting {Key} created", setting.Key);
            return ApiResponse.Created(setting, "Setting created").ToResult();
        })
        .RequirePermission(Permissions.SettingsUpdate);

        // PUT /api/settings/{key} — update, requires settings.update
        group.MapPut("/{key}", async (string key, UpdateSettingRequest req, ISettingService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/settings/{Key} — updating", key);
            var setting = await service.UpdateAsync(key, req, null);
            if (setting == null)
                return ApiResponse.Error($"Setting '{key}' not found", 404).ToResult();
            log.LogInformation("Setting {Key} updated", key);
            return ApiResponse.Success(setting, "Setting updated").ToResult();
        })
        .RequirePermission(Permissions.SettingsUpdate);

        // DELETE /api/settings/{key} — requires settings.update
        group.MapDelete("/{key}", async (string key, ISettingService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/settings/{Key} — deleting", key);
            await service.DeleteAsync(key);
            log.LogInformation("Setting {Key} deleted", key);
            return ApiResponse.Success(null, "Setting deleted").ToResult();
        })
        .RequirePermission(Permissions.SettingsUpdate);
    }
}
