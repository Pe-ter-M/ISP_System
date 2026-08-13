using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Customers.Interfaces;
using InternetProvider.Api.Modules.Customers.Dtos;

namespace InternetProvider.Api.Modules.Customers.Core;

public static class CustomerEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", async (
            int? page, int? pageSize, string? search, string? sortBy, bool? sortDesc, string? subscription,
            ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            page ??= 1; pageSize ??= 10;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            log.LogInformation("GET /api/customers?page={Page}&size={Size}&subscription={Subscription}", page, pageSize, subscription);
            var result = await service.GetAllAsync(page.Value, pageSize.Value, search, sortBy, sortDesc ?? false, subscription);
            return ApiResponse.Success(result, "Customers retrieved").ToResult();
        })
        .RequirePermission(Permissions.CustomersView);

        group.MapGet("/{id:int}", async (int id, ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/customers/{CustomerId}", id);
            var customer = await service.GetByIdAsync(id);
            return ApiResponse.Success(customer, "Customer found").ToResult();
        })
        .RequirePermission(Permissions.CustomersView);

        group.MapPost("/", async (CreateCustomerRequest req, ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/customers — creating {FullName}", req.FullName);
            try
            {
                var customer = await service.CreateAsync(req);
                return ApiResponse.Created(customer, "Customer created successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.CustomersCreate);

        group.MapPut("/{id:int}", async (int id, UpdateCustomerRequest req, ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/customers/{CustomerId} — updating customer", id);
            try
            {
                var customer = await service.UpdateAsync(id, req);
                log.LogInformation("Customer {CustomerId} updated successfully", id);
                return ApiResponse.Success(customer, "Customer updated successfully").ToResult();
            }
            catch (ConflictException ex)
            {
                return ApiResponse.Error(ex.Message, 409).ToResult();
            }
        })
        .RequirePermission(Permissions.CustomersUpdate);

        group.MapDelete("/{id:int}", async (int id, ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/customers/{CustomerId} called", id);
            var result = await service.DeleteAsync(id);
            log.LogInformation("Customer {CustomerId} delete completed (hard: {HardDeleted})", id, result.HardDeleted);
            return ApiResponse.Success(result, result.Message).ToResult();
        })
        .RequirePermission(Permissions.CustomersDelete);
    }
}
