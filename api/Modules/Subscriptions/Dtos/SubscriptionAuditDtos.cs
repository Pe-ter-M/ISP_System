namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

/// <summary>
/// Identifies who performed an action (created or updated a subscription).
/// Staff details are resolved from the linked User account; a customer-created
/// subscription shows only that it was done by the customer themselves.
/// </summary>
public record ActorInfo(
    string Type,          // "staff" | "customer" | "unknown"
    string? FullName,
    string? Role,
    string? Email,
    string? Phone,
    string? StaffCode
);

/// <summary>A single recorded change to a subscription (who, what, when).</summary>
public record SubscriptionHistoryItem(
    int Id,
    ActorInfo? ChangedBy,
    string Change,
    string? Notes,
    DateTime ChangedAt
);
