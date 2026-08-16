namespace InternetProvider.Api.Modules.Subscriptions.Dtos;

public record UpdateSubscriptionRequest(
    string? Status, // e.g. "active", "suspended", "expired"
    DateTime? CurrentPeriodEnd,
    bool? AutoRenew,
    int? PackageId // Allows changing package plan
);