using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Staff.Interfaces;
using InternetProvider.Api.Modules.Staff.Dtos;
using InternetProvider.Api.Modules.Audit.Interfaces;

namespace InternetProvider.Api.Modules.Staff.Core;

public static class StaffEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/staff").WithTags("Staff");

        group.MapGet("/", async (
            int? page, int? pageSize, string? search, string? sortBy, bool? sortDesc,
            IStaffService service, ILogger<LoggerMarker> log) =>
        {
            page ??= 1; pageSize ??= 10;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            log.LogInformation("GET /api/staff?page={Page}&size={Size}", page, pageSize);
            var result = await service.GetAllAsync(page.Value, pageSize.Value, search, sortBy, sortDesc ?? false);
            return ApiResponse.Success(result, "Staff retrieved").ToResult();
        })
        .RequirePermission(Permissions.StaffView);

        group.MapGet("/stats", async (IStaffService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/staff/stats called");
            var stats = await service.GetStatsAsync();
            return ApiResponse.Success(stats, "Staff stats retrieved").ToResult();
        })
        .RequirePermission(Permissions.StaffView);

        group.MapGet("/{id:int}", async (int id, IStaffService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/staff/{StaffId}", id);
            var staff = await service.GetByIdAsync(id);
            return ApiResponse.Success(staff, "Staff member found").ToResult();
        })
        .RequirePermission(Permissions.StaffView);

        group.MapPost("/", async (CreateStaffRequest req, IStaffService service, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/staff — creating {FullName}", req.FullName);
            try
            {
                var staff = await service.CreateAsync(req);
                await audit.RecordAsync("staff", staff.Id, "create", $"Staff member '{staff.FullName}' created");
                return ApiResponse.Created(staff, "Staff member created successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.StaffCreate);

        group.MapPut("/{id:int}", async (int id, UpdateStaffRequest req, IStaffService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/staff/{StaffId} — updating staff member", id);
            try
            {
                var staff = await service.UpdateAsync(id, req);
                log.LogInformation("Staff member {StaffId} updated successfully", id);
                return ApiResponse.Success(staff, "Staff member updated successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.StaffUpdate);

        group.MapDelete("/{id:int}", async (int id, IStaffService service, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/staff/{StaffId} called", id);
            var result = await service.DeleteAsync(id);
            log.LogInformation("Staff member {StaffId} delete completed (hard: {HardDeleted})", id, result.HardDeleted);
            await audit.RecordAsync("staff", id, "delete", $"Staff member #{id} deleted");
            return ApiResponse.Success(result, result.Message).ToResult();
        })
        .RequirePermission(Permissions.StaffDelete);
    }
}
