using Microsoft.EntityFrameworkCore;

namespace InternetProvider.Api.Modules.Infrastructure.Core;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Business tables
    public DbSet<Organization.Core.Models.Organization> Organizations => Set<Organization.Core.Models.Organization>();
    public DbSet<Users.Core.Models.User> Users => Set<Users.Core.Models.User>();
    public DbSet<Roles.Core.Models.Role> Roles => Set<Roles.Core.Models.Role>();
    public DbSet<Roles.Core.Models.Permission> Permissions => Set<Roles.Core.Models.Permission>();
    public DbSet<Roles.Core.Models.RolePermission> RolePermissions => Set<Roles.Core.Models.RolePermission>();
    public DbSet<Roles.Core.Models.UserPermission> UserPermissions => Set<Roles.Core.Models.UserPermission>();
    public DbSet<Customers.Core.Models.Customer> Customers => Set<Customers.Core.Models.Customer>();
    public DbSet<Plans.Core.Models.RadiusGroup> RadiusGroups => Set<Plans.Core.Models.RadiusGroup>();
    public DbSet<Plans.Core.Models.RadiusPackage> RadiusPackages => Set<Plans.Core.Models.RadiusPackage>();
    public DbSet<Subscriptions.Core.Models.Subscription> Subscriptions => Set<Subscriptions.Core.Models.Subscription>();
    public DbSet<Settings.Core.Models.Setting> Settings => Set<Settings.Core.Models.Setting>();
    public DbSet<Payments.Core.Models.Payment> Payments => Set<Payments.Core.Models.Payment>();

    // FreeRADIUS tables
    public DbSet<Radius.Core.Models.RadCheck> RadChecks => Set<Radius.Core.Models.RadCheck>();
    public DbSet<Radius.Core.Models.RadReply> RadReplies => Set<Radius.Core.Models.RadReply>();
    public DbSet<Radius.Core.Models.RadGroupCheck> RadGroupChecks => Set<Radius.Core.Models.RadGroupCheck>();
    public DbSet<Radius.Core.Models.RadGroupReply> RadGroupReplies => Set<Radius.Core.Models.RadGroupReply>();
    public DbSet<Radius.Core.Models.RadUserGroup> RadUserGroups => Set<Radius.Core.Models.RadUserGroup>();
    public DbSet<Nas.Core.Models.NasClient> NasClients => Set<Nas.Core.Models.NasClient>();
    public DbSet<RadAcct.Core.Models.RadAcct> RadAccts => Set<RadAcct.Core.Models.RadAcct>();
    public DbSet<RadPostAuth.Core.Models.RadPostAuth> RadPostAuths => Set<RadPostAuth.Core.Models.RadPostAuth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Organization ──
        modelBuilder.Entity<Organization.Core.Models.Organization>()
            .HasIndex(o => o.Id)
            .IsUnique();

        // ── Users ──
        modelBuilder.Entity<Users.Core.Models.User>()
            .HasIndex(u => u.Email)
            .IsUnique();
        modelBuilder.Entity<Users.Core.Models.User>()
            .HasIndex(u => u.RoleId);

        // ── Roles ──
        modelBuilder.Entity<Roles.Core.Models.Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // ── RolePermissions (composite key) ──
        modelBuilder.Entity<Roles.Core.Models.RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // ── UserPermissions (composite key + index) ──
        modelBuilder.Entity<Roles.Core.Models.UserPermission>()
            .HasKey(up => new { up.UserId, up.PermissionId });
        modelBuilder.Entity<Roles.Core.Models.UserPermission>()
            .HasIndex(up => up.UserId);

        // ── Permissions ──
        modelBuilder.Entity<Roles.Core.Models.Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        // ── Customers ──
        modelBuilder.Entity<Customers.Core.Models.Customer>()
            .HasIndex(c => c.CustomerCode)
            .IsUnique();
        modelBuilder.Entity<Customers.Core.Models.Customer>()
            .HasIndex(c => c.UserId)
            .IsUnique();
        modelBuilder.Entity<Customers.Core.Models.Customer>()
            .HasIndex(c => c.Status);

        // ── Radius Groups ──
        modelBuilder.Entity<Plans.Core.Models.RadiusGroup>()
            .HasIndex(g => g.GroupName)
            .IsUnique();

        // ── Radius Packages ──
        modelBuilder.Entity<Plans.Core.Models.RadiusPackage>()
            .HasIndex(p => p.Name)
            .IsUnique();
        modelBuilder.Entity<Plans.Core.Models.RadiusPackage>()
            .HasIndex(p => p.RadiusGroupId);

        // ── Subscriptions ──
        modelBuilder.Entity<Subscriptions.Core.Models.Subscription>()
            .HasIndex(s => s.Username)
            .IsUnique();
        modelBuilder.Entity<Subscriptions.Core.Models.Subscription>()
            .HasIndex(s => s.CustomerId);
        modelBuilder.Entity<Subscriptions.Core.Models.Subscription>()
            .HasIndex(s => s.Status);
        modelBuilder.Entity<Subscriptions.Core.Models.Subscription>()
            .HasIndex(s => s.CurrentPeriodEnd);

        // ── Settings ──
        modelBuilder.Entity<Settings.Core.Models.Setting>()
            .HasKey(s => s.Key);

        // ── FreeRADIUS indexes ──
        modelBuilder.Entity<Radius.Core.Models.RadCheck>()
            .HasIndex(r => new { r.UserName, r.Attribute });
        modelBuilder.Entity<Radius.Core.Models.RadReply>()
            .HasIndex(r => new { r.UserName, r.Attribute });
        modelBuilder.Entity<Radius.Core.Models.RadGroupCheck>()
            .HasIndex(r => new { r.GroupName, r.Attribute });
        modelBuilder.Entity<Radius.Core.Models.RadGroupReply>()
            .HasIndex(r => new { r.GroupName, r.Attribute });
        modelBuilder.Entity<Radius.Core.Models.RadUserGroup>()
            .HasIndex(r => r.UserName);
        modelBuilder.Entity<Nas.Core.Models.NasClient>()
            .HasIndex(n => n.Nasname)
            .IsUnique();
        modelBuilder.Entity<RadAcct.Core.Models.RadAcct>()
            .HasIndex(a => a.AcctUniqueId)
            .IsUnique();
        modelBuilder.Entity<RadAcct.Core.Models.RadAcct>()
            .HasIndex(a => new { a.AcctStartTime, a.UserName });
        modelBuilder.Entity<RadPostAuth.Core.Models.RadPostAuth>()
            .HasIndex(a => a.Username);
    }
}
