using InternetProvider.Api.Modules.Roles.Core.Models;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Infrastructure;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // ── 1. Sync permissions from Permissions.cs constants ──
        var existingPerms = db.Permissions.ToDictionary(p => p.Code);

        foreach (var (code, description) in Permissions.All)
        {
            if (!existingPerms.ContainsKey(code))
            {
                db.Permissions.Add(new Permission
                {
                    Code = code,
                    Group = code.Split('.')[0],
                    Description = description
                });
            }
        }
        await db.SaveChangesAsync();

        // Reload to get IDs
        var allPerms = db.Permissions.ToDictionary(p => p.Code, p => p.Id);

        // ── 2. Seed default roles ──
        var adminRole       = await EnsureRoleAsync(db, RoleNames.Of(SystemRole.Admin),           true, "Full system access");
        var secretaryRole   = await EnsureRoleAsync(db, RoleNames.Of(SystemRole.Secretary),       true, "Billing and customer support");
        var headTechRole    = await EnsureRoleAsync(db, RoleNames.Of(SystemRole.HeadTechnician),  true, "Manages technicians and field operations");
        var fieldTechRole   = await EnsureRoleAsync(db, RoleNames.Of(SystemRole.FieldTechnician), true, "Installation and maintenance");
        var customerRole    = await EnsureRoleAsync(db, RoleNames.Of(SystemRole.Customer),        true, "End user with portal access only");

        // ── 3. Assign permissions to roles ──

        // Admin: everything
        await SyncRolePermissionsAsync(db, adminRole.Id, Permissions.All.Keys, allPerms);

        // Secretary: current DB permissions as the default seed set
        await SyncRolePermissionsAsync(db, secretaryRole.Id, new[]
        {
            Permissions.CustomersView, Permissions.CustomersCreate, Permissions.CustomersUpdate,
            Permissions.SubscriptionsView, Permissions.SubscriptionsCreate,
            Permissions.SubscriptionsUpdate, Permissions.SubscriptionsSuspend,
            Permissions.PlansView, Permissions.PlansCreate, Permissions.PlansUpdate, Permissions.PlansDelete,
            Permissions.SessionsView,
            Permissions.UsersView,
            Permissions.SettingsView, Permissions.SettingsUpdate,
            Permissions.FinancialView, Permissions.FinancialCreate,
            Permissions.ReportsView,
            Permissions.DevicesView,
            Permissions.AuditView,
            Permissions.StaffView, Permissions.StaffCreate, Permissions.StaffUpdate,
            Permissions.InfrastructureView,
        }, allPerms);

        // Head Technician: current DB permissions as the default seed set
        await SyncRolePermissionsAsync(db, headTechRole.Id, new[]
        {
            Permissions.CustomersView, Permissions.CustomersCreate, Permissions.CustomersUpdate,
            Permissions.SubscriptionsView, Permissions.SubscriptionsCreate,
            Permissions.SubscriptionsUpdate, Permissions.SubscriptionsSuspend,
            Permissions.PlansView,
            Permissions.RadiusView, Permissions.RadiusNasManage,
            Permissions.SessionsView,
            Permissions.DevicesView, Permissions.DevicesCreate, Permissions.DevicesAssign,
            Permissions.InstallationsManage,
            Permissions.InfrastructureView, Permissions.InfrastructureManage,
            Permissions.TechniciansSchedule, Permissions.TechniciansAssign,
            Permissions.StaffView,
        }, allPerms);

        // Field Technician: current DB permissions as the default seed set
        await SyncRolePermissionsAsync(db, fieldTechRole.Id, new[]
        {
            Permissions.CustomersView,
            Permissions.SubscriptionsView, Permissions.SubscriptionsCreate,
            Permissions.SessionsView,
            Permissions.DevicesView, Permissions.DevicesAssign,
            Permissions.InstallationsManage,
            Permissions.InfrastructureView,
        }, allPerms);

        // Customer: no permissions (portal access is gated by role, not granular perms)
        await SyncRolePermissionsAsync(db, customerRole.Id, Array.Empty<string>(), allPerms);
    }

    private static async Task<Role> EnsureRoleAsync(AppDbContext db, string name, bool isSystem, string description)
    {
        var role = db.Roles.FirstOrDefault(r => r.Name == name);
        if (role == null)
        {
            role = new Role
            {
                Name = name,
                IsSystemRole = isSystem,
                Description = description
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }
        return role;
    }

    private static async Task SyncRolePermissionsAsync(AppDbContext db, int roleId, IEnumerable<string> permissionCodes, Dictionary<string, int> allPerms)
    {
        var targetIds = permissionCodes
            .Where(c => allPerms.ContainsKey(c))
            .Select(c => allPerms[c])
            .ToHashSet();

        var currentIds = db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToHashSet();

        // Add new ones
        var toAdd = targetIds.Except(currentIds).ToList();
        foreach (var permId in toAdd)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permId
            });
        }

        // Remove ones no longer assigned
        var toRemove = currentIds.Except(targetIds).ToList();
        var removeEntries = db.RolePermissions
            .Where(rp => rp.RoleId == roleId && toRemove.Contains(rp.PermissionId));
        db.RolePermissions.RemoveRange(removeEntries);

        if (toAdd.Count > 0 || toRemove.Count > 0)
            await db.SaveChangesAsync();
    }
}
