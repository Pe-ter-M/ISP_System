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
            int? page, int? pageSize, string? search, string? sortBy, bool? sortDesc,
            ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            page ??= 1; pageSize ??= 10;
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            log.LogInformation("GET /api/customers?page={Page}&size={Size}", page, pageSize);
            var result = await service.GetAllAsync(page.Value, pageSize.Value, search, sortBy, sortDesc ?? false);
            return ApiResponse.Success(result, "Customers retrieved").ToResult();
        })
        .RequirePermission(Permissions.CustomersView);

        group.MapGet("/{id:int}", async (int id, ICustomerService service, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/customers/{CustomerId}", id);
            try
            {
                var customer = await service.GetByIdAsync(id);
                return ApiResponse.Success(customer, "Customer found").ToResult();
            }
            catch (NotFoundException)
            {
                return ApiResponse.Error("Customer not found", 404).ToResult();
            }
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
    }
}
