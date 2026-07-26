using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Nas.Interfaces;
using InternetProvider.Api.Modules.Nas.Dtos;

namespace InternetProvider.Api.Modules.Nas.Core;

public static class NasEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/nas").WithTags("NAS Clients");

        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? search,
            string? sortBy,
            bool? sortDesc,
            INasService service,
            ILogger<LoggerMarker> log) =>
        {
            page ??= 1;
            pageSize ??= 10;
            bool desc = sortDesc ?? false;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            log.LogInformation("GET /api/nas?page={Page}&size={Size}&search={Search}&sort={SortBy}&desc={SortDesc}",
                page, pageSize, search, sortBy, desc);

            var result = await service.GetAllAsync(page.Value, pageSize.Value, search, sortBy, desc);
            log.LogInformation("Returning {Count}/{Total} NAS clients", result.Items.Count, result.TotalCount);
            return ApiResponse.Success(result, "NAS clients retrieved").ToResult();
        })
        .RequirePermission(Permissions.RadiusNasManage);

        group.MapGet("/{id:int}", async (int id, INasService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/nas/{NasId} called", id);
            NasResponse nasClient = await service.GetByIdAsync(id);
            log.LogInformation("Returning NAS client {NasId}: {Nasname}", id, nasClient.Nasname);
            return ApiResponse.Success(nasClient, "NAS client found").ToResult();
        })
        .RequirePermission(Permissions.RadiusNasManage);

        group.MapPost("/", async (CreateNasRequest req, INasService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/nas — creating NAS client {Nasname}", req.Nasname);
            var nasClient = await service.CreateAsync(req);
            log.LogInformation("Created NAS client {NasId} — {Nasname}", nasClient.Id, nasClient.Nasname);
            return ApiResponse.Created(nasClient, "NAS client created successfully").ToResult();
        })
        .RequirePermission(Permissions.RadiusNasManage);

        group.MapPut("/{id:int}", async (int id, UpdateNasRequest req, INasService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/nas/{NasId} — updating NAS client", id);
            var nasClient = await service.UpdateAsync(id, req);
            log.LogInformation("Updated NAS client {NasId} — {Nasname}", nasClient.Id, nasClient.Nasname);
            return ApiResponse.Success(nasClient, "NAS client updated successfully").ToResult();
        })
        .RequirePermission(Permissions.RadiusNasManage);

        group.MapDelete("/{id:int}", async (int id, INasService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/nas/{NasId} called", id);
            await service.DeleteAsync(id);
            log.LogDebug("NAS client {NasId} deleted successfully", id);
            return ApiResponse.Success(null, "NAS client deleted successfully").ToResult();
        })
        .RequirePermission(Permissions.RadiusNasManage);
    }
}
