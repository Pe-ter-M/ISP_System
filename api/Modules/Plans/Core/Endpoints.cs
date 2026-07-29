using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Plans.Interfaces;
using InternetProvider.Api.Modules.Plans.Dtos;

namespace InternetProvider.Api.Modules.Plans.Core;

public static class PlanEndpoints
{
    public static void Map(WebApplication app)
    {
        // ── Public endpoints (no auth) ──
        var publicGroup = app.MapGroup("/api/plans").WithTags("Plans Public");

        publicGroup.MapGet("/", async (IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/plans called");
            var plans = await service.GetAllAsync();
            log.LogInformation("Returning {Count} plans", plans.Count);
            return ApiResponse.Success(plans, $"Found {plans.Count} plans").ToResult();
        });

        publicGroup.MapGet("/{id:int}", async (int id, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/plans/{PlanId} called", id);
            var plan = await service.GetDetailByIdAsync(id);
            log.LogInformation("Returning plan detail for {PlanId}: {Name}", id, plan.Name);
            return ApiResponse.Success(plan, "Plan details retrieved").ToResult();
        });

        // ── Admin endpoints (auth required) ──
        var adminGroup = app.MapGroup("/api/admin/plans").WithTags("Plans Admin");

        adminGroup.MapPost("/", async (CreatePlanRequest req, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/admin/plans — creating {Name}", req.Name);
            var plan = await service.CreateAsync(req);
            log.LogInformation("Plan created successfully: {PlanId} — {Name}", plan.Id, plan.Name);
            return ApiResponse.Success(plan, "Plan created successfully").ToResult();
        })
        .RequirePermission(Permissions.PlansCreate);

        adminGroup.MapPut("/{id:int}", async (int id, UpdatePlanRequest req, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/admin/plans/{PlanId} called", id);
            var plan = await service.UpdateAsync(id, req);
            log.LogInformation("Plan {PlanId} updated successfully", id);
            return ApiResponse.Success(plan, "Plan updated successfully").ToResult();
        })
        .RequirePermission(Permissions.PlansUpdate);

        adminGroup.MapDelete("/{id:int}", async (int id, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/admin/plans/{PlanId} called", id);
            await service.DeleteAsync(id);
            log.LogInformation("Plan {PlanId} and associated RADIUS policy deleted", id);
            return ApiResponse.Success(null, "Plan and its RADIUS policies deleted successfully").ToResult();
        })
        .RequirePermission(Permissions.PlansDelete);
    }
}
