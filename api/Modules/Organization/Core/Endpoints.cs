using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InternetProvider.Api.Modules.Organization.Core;

public static class OrganizationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/organization").WithTags("Organization");

        group.MapGet("/", () => "Organization endpoint - Get");
        group.MapPut("/", () => "Organization endpoint - Update");
    }
}
