namespace InternetProvider.Api.Modules.Plans.Dtos;

public class PlanSummaryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceCents { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public int? BandwidthUpKbps { get; set; }
    public int? BandwidthDownKbps { get; set; }
    public int MaxDevices { get; set; }
}
