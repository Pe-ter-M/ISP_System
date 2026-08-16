using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Roles.Core.Models;
using InternetProvider.Api.Modules.Audit.Interfaces;

namespace InternetProvider.Api.Modules.Roles.Core;

public static class RoleEndpoints
{
    public static void Map(WebApplication app)
    {
        var roles = app.MapGroup("/api/roles").WithTags("Roles");

        roles.MapGet("/", async (AppDbContext db, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/roles");
            var list = await db.Roles
                .OrderBy(r => r.Name)
                .Select(r => new { r.Id, r.Name, r.IsSystemRole, r.Description })
                .ToListAsync();
            return ApiResponse.Success(list, "Roles retrieved").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);

        roles.MapDelete("/{roleId:int}", async (int roleId, AppDbContext db, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("DELETE /api/roles/{RoleId}", roleId);

            var role = await db.Roles.FindAsync(roleId);
            if (role is null)
                return ApiResponse.Error("Role not found", 404).ToResult();

            if (role.IsSystemRole)
                return ApiResponse.Error("System roles cannot be deleted", 400).ToResult();

            if (await db.Users.AnyAsync(u => u.RoleId == roleId))
                return ApiResponse.Error("Role cannot be deleted because it is assigned to one or more users", 409).ToResult();

            var rolePerms = db.RolePermissions.Where(rp => rp.RoleId == roleId);
            db.RolePermissions.RemoveRange(rolePerms);
            db.Roles.Remove(role);

            await db.SaveChangesAsync();
            log.LogInformation("Role deleted: {RoleId}", roleId);
            await audit.RecordAsync("role", roleId, "delete", $"Role '{role.Name}' deleted");
            return ApiResponse.Success(new { roleId }, "Role deleted").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);

        roles.MapPost("/", async (CreateRoleRequest req, AppDbContext db, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("POST /api/roles — creating role {Name}", req.Name);
            if (await db.Roles.AnyAsync(r => r.Name == req.Name))
                return ApiResponse.Error("Role already exists", 409).ToResult();

            var role = new Role { Name = req.Name, Description = req.Description };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            log.LogInformation("Role created: {RoleId} — {Name}", role.Id, role.Name);
            await audit.RecordAsync("role", role.Id, "create", $"Role '{role.Name}' created");
            return ApiResponse.Created(new { role.Id, role.Name, role.Description }, "Role created").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);

        // ── Role permissions ──

        var rolePerms = app.MapGroup("/api/roles/{roleId:int}/permissions").WithTags("Role Permissions");

        rolePerms.MapGet("/", async (int roleId, AppDbContext db, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/roles/{RoleId}/permissions", roleId);

            if (!await db.Roles.AnyAsync(r => r.Id == roleId))
                return ApiResponse.Error("Role not found", 404).ToResult();

            var perms = await db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Join(db.Permissions, rp => rp.PermissionId, p => p.Id,
                    (_, p) => new { p.Id, p.Code, p.Group, p.Description })
                .OrderBy(p => p.Group).ThenBy(p => p.Code)
                .ToListAsync();

            return ApiResponse.Success(perms, "Role permissions retrieved").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);

        // Accepts permission codes (same convention as user permissions endpoint)
        rolePerms.MapPut("/", async (int roleId, SetRolePermissionsRequest req, AppDbContext db, IAuditService audit, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("PUT /api/roles/{RoleId}/permissions — {Count} codes", roleId, req.Codes.Count);

            if (!await db.Roles.AnyAsync(r => r.Id == roleId))
                return ApiResponse.Error("Role not found", 404).ToResult();

            var foundPerms = await db.Permissions
                .Where(p => req.Codes.Contains(p.Code))
                .ToDictionaryAsync(p => p.Code, p => p.Id);

            var unknown = req.Codes.Except(foundPerms.Keys).ToList();
            if (unknown.Count > 0)
                return ApiResponse.Error($"Unknown permission codes: {string.Join(", ", unknown)}", 400).ToResult();

            var existing = db.RolePermissions.Where(rp => rp.RoleId == roleId);
            db.RolePermissions.RemoveRange(existing);

            foreach (var id in foundPerms.Values)
                db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = id });

            await db.SaveChangesAsync();
            log.LogInformation("Role {RoleId} permissions updated: {Count} permissions set", roleId, foundPerms.Count);
            var roleName = await db.Roles.AsNoTracking().Where(r => r.Id == roleId).Select(r => r.Name).FirstOrDefaultAsync();
            await audit.RecordAsync("role", roleId, "update", $"Permissions for role '{roleName}' updated ({foundPerms.Count} permissions)");
            return ApiResponse.Success(new { count = foundPerms.Count }, "Role permissions updated").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);

        // ── Permissions catalogue ──

        var perms = app.MapGroup("/api/permissions").WithTags("Permissions");

        perms.MapGet("/", async (AppDbContext db, ILogger<LoggerMarker> log) =>
        {
            log.LogInformation("GET /api/permissions");
            var list = await db.Permissions
                .OrderBy(p => p.Group).ThenBy(p => p.Code)
                .Select(p => new { p.Id, p.Code, p.Group, p.Description })
                .ToListAsync();
            return ApiResponse.Success(list, "Permissions retrieved").ToResult();
        })
        .RequirePermission(Permissions.RolesManage);
    }
}

public record CreateRoleRequest(string Name, string? Description);
public record SetRolePermissionsRequest(List<string> Codes);

