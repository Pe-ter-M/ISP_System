using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Radius.Core;

public static class RadiusEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/radius").WithTags("Radius");

        group.MapGet("/radcheck", () => "Radius endpoint - List radcheck");
        group.MapGet("/radusergroup", () => "Radius endpoint - List radusergroup");
        group.MapGet("/radgroupreply", () => "Radius endpoint - List radgroupreply");
    }
}
