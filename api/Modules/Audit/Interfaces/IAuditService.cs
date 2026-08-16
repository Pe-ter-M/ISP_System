using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Audit.Dtos;

namespace InternetProvider.Api.Modules.Audit.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Record a SUCCESSFUL mutation (or login attempt) as a centralized audit entry.
    /// Actor (current user) and IP are resolved automatically from the HTTP context.
    /// Call this only after an operation has succeeded.
    /// </summary>
    Task RecordAsync(
        string entityType,
        int? entityId,
        string action,
        string summary,
        IReadOnlyList<AuditChange>? changes = null,
        int? actorUserId = null,
        string? actorType = null);

    /// <summary>Paginated audit log listing.</summary>
    Task<PaginatedResponse<AuditLogListItem>> GetPagedAsync(AuditLogQuery query);

    /// <summary>Full detail of a single audit entry (with actor + entity label + changes).</summary>
    Task<AuditLogDetail?> GetByIdAsync(long id);
}
