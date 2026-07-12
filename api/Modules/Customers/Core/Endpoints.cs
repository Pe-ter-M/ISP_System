using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Customers.Core;

public static class CustomerEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/customers").WithTags("Customers");

        group.MapGet("/", () => "Customers endpoint - List");
        group.MapGet("/{id:int}", (int id) => $"Customers endpoint - Get {id}");
        group.MapPost("/", () => "Customers endpoint - Create");
        group.MapPut("/{id:int}", (int id) => $"Customers endpoint - Update {id}");
        group.MapDelete("/{id:int}", (int id) => $"Customers endpoint - Delete {id}");
    }
}
