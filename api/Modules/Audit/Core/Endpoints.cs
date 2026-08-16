using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Audit.Interfaces;
using InternetProvider.Api.Modules.Audit.Dtos;

namespace InternetProvider.Api.Modules.Audit.Core;

public static class AuditEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/audit").WithTags("Audit");

        // ── GET: Paginated audit log (paging, filters, search, sort) ──
        group.MapGet("/", async (
            int? page,
            int? pageSize,
            string? entityType,
            string? action,
            string? search,
            string? sortBy,
            bool? sortDesc,
            IAuditService service,
            ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/audit?page={Page}&size={PageSize}&entity={Entity}&action={Action}&search={Search}",
                page, pageSize, entityType, action, search);

            var query = new AuditLogQuery(
                Page: Math.Max(page ?? 1, 1),
                PageSize: Math.Clamp(pageSize ?? 20, 1, 100),
                EntityType: entityType,
                Action: action,
                Search: search,
                SortBy: sortBy,
                SortDesc: sortDesc ?? true);

            var result = await service.GetPagedAsync(query);
            return ApiResponse.Success(result, $"Found {result.TotalCount} audit entries").ToResult();
        })
        .RequirePermission(Permissions.AuditView);

        // ── GET: Single audit entry detail ──
        group.MapGet("/{id:long}", async (long id, IAuditService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/audit/{Id} called", id);
            var item = await service.GetByIdAsync(id);
            if (item == null)
                return ApiResponse.Error("Audit entry not found", 404).ToResult();
            return ApiResponse.Success(item, "Audit entry retrieved").ToResult();
        })
        .RequirePermission(Permissions.AuditView);
    }
}
