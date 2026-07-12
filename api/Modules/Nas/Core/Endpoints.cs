using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Nas.Core;

public static class NasEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/nas").WithTags("NAS Clients");

        group.MapGet("/", () => "NAS endpoint - List");
        group.MapGet("/{id:int}", (int id) => $"NAS endpoint - Get {id}");
        group.MapPost("/", () => "NAS endpoint - Create");
        group.MapPut("/{id:int}", (int id) => $"NAS endpoint - Update {id}");
        group.MapDelete("/{id:int}", (int id) => $"NAS endpoint - Delete {id}");
    }
}
