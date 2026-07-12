using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Plans.Core;

public static class PlanEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/plans").WithTags("Plans");
        var groups = app.MapGroup("/api/radius-groups").WithTags("Radius Groups");

        // Packages
        group.MapGet("/", () => "Plans endpoint - List");
        group.MapGet("/{id:int}", (int id) => $"Plans endpoint - Get {id}");
        group.MapPost("/", () => "Plans endpoint - Create");
        group.MapPut("/{id:int}", (int id) => $"Plans endpoint - Update {id}");
        group.MapDelete("/{id:int}", (int id) => $"Plans endpoint - Delete {id}");

        // Groups
        groups.MapGet("/", () => "Radius Groups endpoint - List");
        groups.MapGet("/{id:int}", (int id) => $"Radius Groups endpoint - Get {id}");
        groups.MapPost("/", () => "Radius Groups endpoint - Create");
        groups.MapPut("/{id:int}", (int id) => $"Radius Groups endpoint - Update {id}");
        groups.MapDelete("/{id:int}", (int id) => $"Radius Groups endpoint - Delete {id}");
    }
}
