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
    string PaymentStatus
);