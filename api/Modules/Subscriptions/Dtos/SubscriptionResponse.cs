namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

public record SubscriptionResponse(
    int Id,
    int CustomerId,
    int PackageId,
    string Username,
    string Status,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    bool AutoRenew,
    
    // ── Associated Payment Log Details ──
    int PaidAmountCents,
    string? PaymentReference,
    string PaymentStatus,
    string? PaymentMethod,
    DateTime? PaymentCompletedAt,

    // ── Friendly display fields ──
    string CustomerFullName,
    string CustomerCode,
    string PlanName
)
{
    /// <summary>Who created this subscription (staff actor or customer self-service).</summary>
    public ActorInfo? CreatedByInfo { get; set; }

    /// <summary>Who last updated this subscription (staff only).</summary>
    public ActorInfo? UpdatedByInfo { get; set; }

    /// <summary>Timestamp of the last update, if any.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Chronological change history (who changed what, and when).</summary>
    public List<SubscriptionHistoryItem> AuditHistory { get; set; } = new();
}
