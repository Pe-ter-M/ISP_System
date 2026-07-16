using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Roles.Core.Models;

namespace InternetProvider.Api.Modules.Roles.Core;

public static class RoleEndpoints
{
    public static void Map(WebApplication app)
    {
        var roles = app.MapGroup("/api/roles").WithTags("Roles");

        roles.MapGet("/", async (AppDbContext db) =>
        {
            var list = await db.Roles
                .OrderBy(r => r.Name)
                .Select(r => new { r.Id, r.Name, r.IsSystemRole, r.Description })
                .ToListAsync();
            return Results.Ok(list);
        })
        .RequirePermission(Permissions.RolesManage);

        roles.MapPost("/", async (CreateRoleRequest req, AppDbContext db) =>
        {
            if (await db.Roles.AnyAsync(r => r.Name == req.Name))
                return Results.Conflict(new { error = "Role already exists" });

            var role = new Role { Name = req.Name, Description = req.Description };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            return Results.Created($"/api/roles/{role.Id}", new { role.Id, role.Name, role.Description });
        })
        .RequirePermission(Permissions.RolesManage);

        var rolePerms = app.MapGroup("/api/roles/{roleId:int}/permissions").WithTags("Role Permissions");

        rolePerms.MapGet("/", async (int roleId, AppDbContext db) =>
        {
            var permIds = await db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var perms = await db.Permissions
                .Where(p => permIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.Group, p.Description })
                .ToListAsync();

            return Results.Ok(perms);
        })
        .RequirePermission(Permissions.RolesManage);

        rolePerms.MapPut("/", async (int roleId, SetRolePermissionsRequest req, AppDbContext db) =>
        {
            var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            db.RolePermissions.RemoveRange(existing);

            foreach (var permId in req.PermissionIds)
            {
                db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permId });
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Permissions updated" });
        })
        .RequirePermission(Permissions.RolesManage);

        var perms = app.MapGroup("/api/permissions").WithTags("Permissions");

        perms.MapGet("/", async (AppDbContext db) =>
        {
            var list = await db.Permissions.OrderBy(p => p.Group).ThenBy(p => p.Code)
                .Select(p => new { p.Id, p.Code, p.Group, p.Description })
                .ToListAsync();
            return Results.Ok(list);
        })
        .RequirePermission(Permissions.RolesManage);
    }
}

public record CreateRoleRequest(string Name, string? Description);
public record SetRolePermissionsRequest(List<int> PermissionIds);
