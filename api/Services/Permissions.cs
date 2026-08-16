namespace InternetProvider.Api.Services;

public static class Permissions
{
    // ── Users ──
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    // ── Customers ──
    public const string CustomersView = "customer.view";
    public const string CustomersCreate = "customer.create";
    public const string CustomersUpdate = "customer.update";
    public const string CustomersDelete = "customer.delete";

    // ── Staff ──
    public const string StaffView = "staff.view";
    public const string StaffCreate = "staff.create";
    public const string StaffUpdate = "staff.update";
    public const string StaffDelete = "staff.delete";

    // ── Subscriptions ──
    public const string SubscriptionsView = "subscription.view";
    public const string SubscriptionsCreate = "subscription.create";
    public const string SubscriptionsUpdate = "subscription.update";
    public const string SubscriptionsDelete = "subscription.delete";
    public const string SubscriptionsSuspend = "subscription.suspend";

    // ── Plans ──
    public const string PlansView = "plan.view";
    public const string PlansCreate = "plan.create";
    public const string PlansUpdate = "plan.update";
    public const string PlansDelete = "plan.delete";

    // ── RADIUS ──
    public const string RadiusView = "radius.view";
    public const string RadiusNasManage = "radius.nas.manage";
    public const string RadiusGroupsManage = "radius.groups.manage";

    // ── Sessions ──
    public const string SessionsView = "session.view";

    // ── Settings ──
    public const string SettingsView = "settings.view";
    public const string SettingsUpdate = "settings.update";

    // ── Financial ──
    public const string FinancialView = "financial.view";
    public const string FinancialCreate = "financial.create";

    // ── Devices ──
    public const string DevicesView = "device.view";
    public const string DevicesCreate = "device.create";
    public const string DevicesAssign = "device.assign";

    // ── Installations ──
    public const string InstallationsManage = "installation.manage";

    // ── Infrastructure ──
    public const string InfrastructureView = "infrastructure.view";
    public const string InfrastructureManage = "infrastructure.manage";

    // ── Technicians ──
    public const string TechniciansSchedule = "technician.schedule";
    public const string TechniciansAssign = "technician.assign";

    // ── Roles & Permissions ──
    public const string RolesManage = "role.manage";

    // ── Audit ──
    public const string AuditView = "audit.view";

    // ── Reports ──
    public const string ReportsView = "report.view";

    public static readonly Dictionary<string, string> All = new()
    {
        { UsersView, "View user list" },
        { UsersCreate, "Create users" },
        { UsersUpdate, "Edit users" },
        { UsersDelete, "Delete users" },
        { CustomersView, "View customers" },
        { CustomersCreate, "Create customers" },
        { CustomersUpdate, "Edit customers" },
        { CustomersDelete, "Delete customers" },
        { StaffView, "View staff members" },
        { StaffCreate, "Create staff members" },
        { StaffUpdate, "Edit staff members" },
        { StaffDelete, "Delete staff members" },
        { SubscriptionsView, "View subscriptions" },
        { SubscriptionsCreate, "Create subscriptions" },
        { SubscriptionsUpdate, "Edit subscriptions" },
        { SubscriptionsDelete, "Delete subscriptions" },
        { SubscriptionsSuspend, "Suspend/unsuspend subscriptions" },
        { PlansView, "View plans and groups" },
        { PlansCreate, "Create plans" },
        { PlansUpdate, "Edit plans" },
        { PlansDelete, "Delete plans" },
        { RadiusView, "View RADIUS tables" },
        { RadiusNasManage, "Manage NAS clients" },
        { RadiusGroupsManage, "Manage RADIUS groups" },
        { SessionsView, "View live sessions" },
        { SettingsView, "View settings" },
        { SettingsUpdate, "Update settings" },
        { FinancialView, "View financial records" },
        { FinancialCreate, "Record payments" },
        { DevicesView, "View inventory" },
        { DevicesCreate, "Add inventory" },
        { DevicesAssign, "Assign devices" },
        { InstallationsManage, "Manage installations" },
        { InfrastructureView, "View infrastructure" },
        { InfrastructureManage, "Manage infrastructure" },
        { TechniciansSchedule, "Manage technician schedules" },
        { TechniciansAssign, "Assign technician jobs" },
        { RolesManage, "Manage roles and permissions" },
        { AuditView, "View audit log" },
        { ReportsView, "View reports" },
    };

    /// <summary>
    /// Enforce the view-first rule: any non-view permission (e.g. "customer.update")
    /// requires its resource's "view" permission (e.g. "customer.view") to be present,
    /// but only when a "view" permission actually exists for that resource (e.g.
    /// "technician" / "role" / "installation" have no view permission and are standalone).
    /// A "view" permission may be granted alone, but not the other way around.
    /// Returns an error message describing the first violation, or null if valid.
    /// </summary>
    public static string? ValidateViewDependency(IEnumerable<string> codes)
    {
        var set = codes.ToHashSet();
        foreach (var code in set)
        {
            var dot = code.IndexOf('.');
            if (dot <= 0) continue;
            var resource = code[..dot];
            var action = code[(dot + 1)..];
            if (action.Equals("view", StringComparison.OrdinalIgnoreCase)) continue;

            var viewCode = $"{resource}.view";
            // Only enforce when a real "view" permission exists for this resource.
            if (!All.ContainsKey(viewCode)) continue;
            if (set.Contains(viewCode)) continue;

            return $"Cannot assign '{code}' without '{viewCode}'. The '{resource}' view permission must be granted first.";
        }
        return null;
    }
}
