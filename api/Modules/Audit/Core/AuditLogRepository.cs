using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Audit.Interfaces;
using InternetProvider.Api.Modules.Audit.Core.Models;
using InternetProvider.Api.Modules.Audit.Dtos;

namespace InternetProvider.Api.Modules.Audit.Core;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<AuditLogRepository> _log;

    public AuditLogRepository(AppDbContext db, ILogger<AuditLogRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task AddAsync(AuditLog entry)
    {
        entry.CreatedAt = DateTime.UtcNow;
        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<AuditLog>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? entityType = null,
        string? action = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = true)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType.Trim().ToLower());

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action.Trim().ToLower());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a =>
                a.Summary.ToLower().Contains(term) ||
                a.EntityType.ToLower().Contains(term) ||
                a.IpAddress!.ToLower().Contains(term));
        }

        // Sort: default newest first (created_at desc).
        query = (sortBy?.ToLower()) switch
        {
            "created" => sortDesc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            "entity" => sortDesc ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType),
            "action" => sortDesc ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action),
            _ => sortDesc ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResponse<AuditLog>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<AuditLog?> GetByIdAsync(long id)
    {
        return await _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Dictionary<int, AuditActorInfo>> ResolveActorsAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        var result = new Dictionary<int, AuditActorInfo>();

        if (ids.Count == 0)
            return result;

        // Staff actors (staff -> user -> role).
        var staff = await _db.Staff.AsNoTracking()
            .Include(s => s.User).ThenInclude(u => u!.Role)
            .Where(s => ids.Contains(s.UserId))
            .ToListAsync();

        foreach (var s in staff)
        {
            if (s.User == null) continue;
            result[s.UserId] = new AuditActorInfo(
                "staff",
                s.User.FullName,
                s.User.Role?.Name,
                s.User.Email,
                s.User.Phone,
                s.StaffCode);
        }

        // Remaining users that are customers (customer profile exists).
        var remaining = ids.Where(id => !result.ContainsKey(id)).ToList();
        if (remaining.Count > 0)
        {
            var customerUsers = await _db.Customers.AsNoTracking()
                .Include(c => c.User)
                .Where(c => remaining.Contains(c.UserId))
                .Select(c => new { c.UserId, FullName = c.User != null ? c.User.FullName : null })
                .ToListAsync();

            foreach (var c in customerUsers)
            {
                result[c.UserId] = new AuditActorInfo("customer", c.FullName, null, null, null, null);
            }
        }

        // Any still-unresolved users -> unknown.
        foreach (var id in remaining.Where(id => !result.ContainsKey(id)))
        {
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            result[id] = new AuditActorInfo("unknown", user?.FullName, null, user?.Email, user?.Phone, null);
        }

        return result;
    }

    public async Task<string> ResolveEntityLabelAsync(string entityType, int? entityId)
    {
        if (entityId == null)
            return string.Empty;

        return entityType.ToLower() switch
        {
            "customer" => (await _db.Customers.AsNoTracking()
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == entityId))?.User?.FullName
                    ?? $"Customer #{entityId}",
            "staff" => (await _db.Staff.AsNoTracking()
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.Id == entityId))?.User?.FullName
                    ?? $"Staff #{entityId}",
            "user" => (await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == entityId))?.FullName
                    ?? $"User #{entityId}",
            "role" => (await _db.Roles.AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == entityId))?.Name
                    ?? $"Role #{entityId}",
            "plan" => (await _db.RadiusPackages.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == entityId))?.Name
                    ?? $"Plan #{entityId}",
            "nas" => (await _db.NasClients.AsNoTracking()
                    .FirstOrDefaultAsync(n => n.Id == entityId))?.Shortname
                    ?? $"NAS #{entityId}",
            "setting" => (await _db.Settings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Key == entityId.ToString()))?.Key
                    ?? $"Setting #{entityId}",
            "organization" => (await _db.Organizations.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == entityId))?.Name
                    ?? $"Organization #{entityId}",
            _ => string.Empty,
        };
    }
}
