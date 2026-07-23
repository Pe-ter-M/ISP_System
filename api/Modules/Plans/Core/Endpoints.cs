using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
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

            try
            {
                var plan = await service.GetDetailByIdAsync(id);
                return ApiResponse.Success(plan, "Plan details retrieved").ToResult();
            }
            catch (NotFoundException)
            {
                log.LogWarning("Plan {PlanId} not found", id);
                return ApiResponse.Error("Plan not found", 404).ToResult();
            }
        });

        // ── Admin endpoints (auth required) ──
        var adminGroup = app.MapGroup("/api/admin/plans").WithTags("Plans Admin");

        adminGroup.MapPost("/", async (CreatePlanRequest req, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/admin/plans — creating {Name}", req.Name);

            try
            {
                var plan = await service.CreateAsync(req);
                return ApiResponse.Success(new { plan.Id, plan.Name }, "Plan created successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                log.LogWarning("Conflict creating plan: {Message}", ex.Message);
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.PlansCreate);

        adminGroup.MapPut("/{id:int}", async (int id, UpdatePlanRequest req, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/admin/plans/{PlanId} called", id);

            try
            {
                var plan = await service.UpdateAsync(id, req);
                return ApiResponse.Success(new { plan.Id, plan.Name }, "Plan updated successfully").ToResult();
            }
            catch (NotFoundException)
            {
                return ApiResponse.Error("Plan not found", 404).ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.PlansUpdate);

        adminGroup.MapDelete("/{id:int}", async (int id, IPlanService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/admin/plans/{PlanId} called", id);

            try
            {
                await service.DeleteAsync(id);
                return ApiResponse.Success(null, "Plan deactivated successfully").ToResult();
            }
            catch (NotFoundException)
            {
                return ApiResponse.Error("Plan not found", 404).ToResult();
            }
        })
        .RequirePermission(Permissions.PlansDelete);
    }
}
