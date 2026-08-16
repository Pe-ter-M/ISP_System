namespace InternetProvider.Api.Modules.Audit.Dtos;

/// <summary>Actor display info (reuses the subscription ActorInfo shape).</summary>
public record AuditActorInfo(
    string Type,          // "staff" | "customer" | "system" | "anonymous" | "unknown"
    string? FullName,
    string? Role,
    string? Email,
    string? Phone,
    string? StaffCode
);

/// <summary>One changed field: old -&gt; new.</summary>
public record AuditChange(
    string Field,
    string? OldValue,
    string? NewValue
);

/// <summary>Compact audit row for list views.</summary>
public record AuditLogListItem(
    long Id,
    string EntityType,
    int? EntityId,
    string Action,
    string Summary,
    string? IpAddress,
    DateTime CreatedAt,
    AuditActorInfo? Actor
);

/// <summary>Full detail of a single audit entry (list fields + enriched detail).</summary>
public record AuditLogDetail(
    long Id,
    string EntityType,
    int? EntityId,
    string EntityLabel,
    string Action,
    string Summary,
    string? IpAddress,
    string? HttpMethod,
    string? HttpPath,
    DateTime CreatedAt,
    AuditActorInfo? Actor,
    List<AuditChange> Changes
);

/// <summary>Query parameters for the paginated audit log listing.</summary>
public record AuditLogQuery(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    string? Action = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = true
);
