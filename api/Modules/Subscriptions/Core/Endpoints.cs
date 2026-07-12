using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Subscriptions.Core;

public static class SubscriptionEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        group.MapGet("/", () => "Subscriptions endpoint - List");
        group.MapGet("/{id:int}", (int id) => $"Subscriptions endpoint - Get {id}");
        group.MapPost("/", () => "Subscriptions endpoint - Create");
        group.MapPut("/{id:int}", (int id) => $"Subscriptions endpoint - Update {id}");
        group.MapDelete("/{id:int}", (int id) => $"Subscriptions endpoint - Delete {id}");
    }
}
