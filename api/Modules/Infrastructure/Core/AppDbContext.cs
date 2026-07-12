using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Organization.Core.Models;
using InternetProvider.Api.Modules.Settings.Core.Models;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Customers.Core.Models;
using InternetProvider.Api.Modules.Plans.Core.Models;
using InternetProvider.Api.Modules.Subscriptions.Core.Models;
using InternetProvider.Api.Modules.Radius.Core.Models;
using InternetProvider.Api.Modules.Nas.Core.Models;
using InternetProvider.Api.Modules.RadAcct.Core.Models;
using InternetProvider.Api.Modules.RadPostAuth.Core.Models;

namespace InternetProvider.Api.Modules.Infrastructure.Core;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Business tables
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<RadiusGroup> RadiusGroups => Set<RadiusGroup>();
    public DbSet<RadiusPackage> RadiusPackages => Set<RadiusPackage>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Setting> Settings => Set<Setting>();

    // FreeRADIUS tables
    public DbSet<RadCheck> RadChecks => Set<RadCheck>();
    public DbSet<RadReply> RadReplies => Set<RadReply>();
    public DbSet<RadGroupCheck> RadGroupChecks => Set<RadGroupCheck>();
    public DbSet<RadGroupReply> RadGroupReplies => Set<RadGroupReply>();
    public DbSet<RadUserGroup> RadUserGroups => Set<RadUserGroup>();
    public DbSet<NasClient> NasClients => Set<NasClient>();
    public DbSet<RadAcct> RadAccts => Set<RadAcct>();
    public DbSet<RadPostAuth> RadPostAuths => Set<RadPostAuth>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Organization ──
        modelBuilder.Entity<Organization>()
            .HasIndex(o => o.Id)
            .IsUnique();

        // ── Users ──
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.RoleId);

        // ── Roles ──
        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // ── RolePermissions (composite key) ──
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // ── Permissions ──
        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        // ── Customers ──
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.CustomerCode)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.UserId)
            .IsUnique();

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Status);

        // ── Radius Groups ──
        modelBuilder.Entity<RadiusGroup>()
            .HasIndex(g => g.GroupName)
            .IsUnique();

        // ── Radius Packages ──
        modelBuilder.Entity<RadiusPackage>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<RadiusPackage>()
            .HasIndex(p => p.RadiusGroupId);

        // ── Subscriptions ──
        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.Username)
            .IsUnique();

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.CustomerId);

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.Status);

        modelBuilder.Entity<Subscription>()
            .HasIndex(s => s.CurrentPeriodEnd);

        // ── Settings ──
        modelBuilder.Entity<Setting>()
            .HasKey(s => s.Key);

        // ── FreeRADIUS indexes ──
        modelBuilder.Entity<RadCheck>()
            .HasIndex(r => new { r.UserName, r.Attribute });

        modelBuilder.Entity<RadReply>()
            .HasIndex(r => new { r.UserName, r.Attribute });

        modelBuilder.Entity<RadGroupCheck>()
            .HasIndex(r => new { r.GroupName, r.Attribute });

        modelBuilder.Entity<RadGroupReply>()
            .HasIndex(r => new { r.GroupName, r.Attribute });

        modelBuilder.Entity<RadUserGroup>()
            .HasIndex(r => r.UserName);

        modelBuilder.Entity<NasClient>()
            .HasIndex(n => n.Nasname)
            .IsUnique();

        modelBuilder.Entity<RadAcct>()
            .HasIndex(a => a.AcctUniqueId)
            .IsUnique();

        modelBuilder.Entity<RadAcct>()
            .HasIndex(a => new { a.AcctStartTime, a.UserName });

        modelBuilder.Entity<RadPostAuth>()
            .HasIndex(a => a.Username);
    }
}
