namespace InternetProvider.Api.Modules.Plans.Dtos;

public class PlanDetailResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceCents { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public int? BandwidthUpKbps { get; set; }
    public int? BandwidthDownKbps { get; set; }
    public int SessionTimeoutSeconds { get; set; }
    public int IdleTimeoutSeconds { get; set; }
    public int MaxDevices { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string GroupName { get; set; } = "";
}
