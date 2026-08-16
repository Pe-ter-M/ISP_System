using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Organization.Interfaces;
using InternetProvider.Api.Modules.Organization.Dtos;
using InternetProvider.Api.Modules.Audit.Interfaces;

namespace InternetProvider.Api.Modules.Organization.Core;

public static class OrganizationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/organization").WithTags("Organization");

        group.MapGet("/", async (IOrganizationService service) =>
        {
            var org = await service.GetAsync();
            if (org == null)
                return ApiResponse.Error("Organization not set up", 404).ToResult();
            return ApiResponse.Success(org, "OK").ToResult();
        });

        group.MapPut("/", async (UpdateOrganizationRequest req, IOrganizationService service, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            var org = await service.UpdateAsync(req);
            if (org == null)
                return ApiResponse.Error("Organization not found", 404).ToResult();
            log.LogInformation("Organization settings updated");
            await audit.RecordAsync("organization", org.Id, "update", $"Organization '{org.Name}' updated");
            return ApiResponse.Success(org, "Organization updated").ToResult();
        })
        .RequirePermission(Permissions.SettingsUpdate);
    }
}
