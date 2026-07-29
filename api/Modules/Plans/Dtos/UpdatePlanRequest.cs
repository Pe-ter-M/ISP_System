namespace InternetProvider.Api.Modules.Plans.Dtos;

public record UpdatePlanRequest(
    string? Name,
    string? Description,
    int? RadiusGroupId,
    int? PriceCents,
    string? BillingCycle,
    int? BandwidthUpKbps,
    int? BandwidthDownKbps,
    int? SessionTimeoutSeconds,
    int? IdleTimeoutSeconds,
    int? MaxDevices,
    int? SortOrder,
    bool? IsActive
);
