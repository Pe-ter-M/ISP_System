using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Audit.Core.Models;
using InternetProvider.Api.Modules.Audit.Dtos;

namespace InternetProvider.Api.Modules.Audit.Interfaces;

public interface IAuditLogRepository
{
    /// <summary>Persist a new audit entry (called after a successful operation).</summary>
    Task AddAsync(AuditLog entry);

    /// <summary>Paginated, filterable, searchable, sortable audit log listing (raw rows).</summary>
    Task<PaginatedResponse<AuditLog>> GetPagedAsync(
        int page = 1,
        int pageSize = 20,
        string? entityType = null,
        string? action = null,
        string? search = null,
        string? sortBy = null,
        bool sortDesc = true);

    /// <summary>Fetch a single audit row by id.</summary>
    Task<AuditLog?> GetByIdAsync(long id);

    /// <summary>Resolve actor display info for a set of User.Ids in one query (staff vs customer).</summary>
    Task<Dictionary<int, AuditActorInfo>> ResolveActorsAsync(IEnumerable<int> userIds);

    /// <summary>Resolve a human-friendly label for an entity (type + id), e.g. customer name / plan name.</summary>
    Task<string> ResolveEntityLabelAsync(string entityType, int? entityId);
}
