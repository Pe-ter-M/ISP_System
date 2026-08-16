namespace InternetProvider.Api.Modules.Plans.Dtos;

public record CreatePlanRequest(
    string Name,
    string? Description,
    int PriceCents,
    string? BillingCycle,
    int? BandwidthUpKbps,
    int? BandwidthDownKbps,
    int? SessionTimeoutSeconds,
    int? IdleTimeoutSeconds,
    int? MaxDevices,
    int? SortOrder
);
