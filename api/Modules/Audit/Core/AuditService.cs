using System.Security.Claims;
using System.Text.Json;
using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Modules.Audit.Interfaces;
using InternetProvider.Api.Modules.Audit.Core.Models;
using InternetProvider.Api.Modules.Audit.Dtos;

namespace InternetProvider.Api.Modules.Audit.Core;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;
    private readonly IHttpContextAccessor _http;
    private readonly ILogger<AuditService> _log;

    public AuditService(IAuditLogRepository repo, IHttpContextAccessor http, ILogger<AuditService> log)
    {
        _repo = repo;
        _http = http;
        _log = log;
    }

    public async Task RecordAsync(
        string entityType,
        int? entityId,
        string action,
        string summary,
        IReadOnlyList<AuditChange>? changes = null,
        int? actorUserId = null,
        string? actorType = null)
    {
        try
        {
            var context = _http.HttpContext;

            // Resolve the actor (current authenticated user) unless explicitly provided.
            var userId = actorUserId ?? ResolveUserId(context);
            var type = actorType ?? (userId != null ? "staff" : "anonymous");

            var entry = new AuditLog
            {
                ActorUserId = userId,
                ActorType = type,
                EntityType = entityType.ToLower(),
                EntityId = entityId,
                Action = action.ToLower(),
                Summary = summary,
                Changes = changes != null && changes.Count > 0
                    ? JsonSerializer.Serialize(changes)
                    : null,
                HttpMethod = context?.Request.Method,
                HttpPath = context?.Request.Path.Value,
                IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(entry);
            _log.LogInformation("Audit recorded: {Action} {EntityType}#{EntityId} by user {UserId} ({Summary})",
                entry.Action, entry.EntityType, entry.EntityId, userId, summary);
        }
        catch (Exception ex)
        {
            // Audit must never break the primary operation.
            _log.LogError(ex, "Failed to record audit entry for {Action} {EntityType}", action, entityType);
        }
    }

    public async Task<PaginatedResponse<AuditLogListItem>> GetPagedAsync(AuditLogQuery query)
    {
        var result = await _repo.GetPagedAsync(
            query.Page, query.PageSize, query.EntityType, query.Action, query.Search, query.SortBy, query.SortDesc);

        // Resolve actor info for all rows in a single batch query.
        var actorIds = result.Items.Where(a => a.ActorUserId != null).Select(a => a.ActorUserId!.Value).ToList();
        var actors = await _repo.ResolveActorsAsync(actorIds);

        var items = result.Items.Select(a => new AuditLogListItem(
            a.Id,
            a.EntityType,
            a.EntityId,
            a.Action,
            a.Summary,
            a.IpAddress,
            a.CreatedAt,
            a.ActorUserId != null && actors.TryGetValue(a.ActorUserId.Value, out var actor) ? actor : new AuditActorInfo(a.ActorType, null, null, null, null, null)
        )).ToList();

        return new PaginatedResponse<AuditLogListItem>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<AuditLogDetail?> GetByIdAsync(long id)
    {
        var entry = await _repo.GetByIdAsync(id);
        if (entry == null)
            return null;

        AuditActorInfo? actor = null;
        if (entry.ActorUserId != null)
        {
            var actors = await _repo.ResolveActorsAsync(new[] { entry.ActorUserId.Value });
            actors.TryGetValue(entry.ActorUserId.Value, out actor);
        }
        actor ??= new AuditActorInfo(entry.ActorType, null, null, null, null, null);

        var changes = DeserializeChanges(entry.Changes);
        var entityLabel = await _repo.ResolveEntityLabelAsync(entry.EntityType, entry.EntityId);

        return new AuditLogDetail(
            entry.Id,
            entry.EntityType,
            entry.EntityId,
            entityLabel,
            entry.Action,
            entry.Summary,
            entry.IpAddress,
            entry.HttpMethod,
            entry.HttpPath,
            entry.CreatedAt,
            actor,
            changes);
    }

    private static int? ResolveUserId(HttpContext? context)
    {
        var principal = context?.Items["User"] as ClaimsPrincipal;
        var claimUserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal?.FindFirst("sub")?.Value;
        return int.TryParse(claimUserId, out var id) ? id : null;
    }

    private static List<AuditChange> DeserializeChanges(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<AuditChange>();
        try
        {
            return JsonSerializer.Deserialize<List<AuditChange>>(json) ?? new List<AuditChange>();
        }
        catch
        {
            return new List<AuditChange>();
        }
    }
}
