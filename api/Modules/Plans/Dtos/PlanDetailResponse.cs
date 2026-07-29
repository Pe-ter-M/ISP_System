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

    // Added friendly presentation properties for Frontend (KES formatting)
    public double Price => PriceCents / 100.0;
    public string PriceFormatted => $"KES {Price:N2}";
    
    public string DownloadSpeedFormatted => BandwidthDownKbps.HasValue 
        ? (BandwidthDownKbps.Value >= 1000 ? $"{BandwidthDownKbps.Value / 1000.0} Mbps" : $"{BandwidthDownKbps.Value} Kbps") 
        : "Unlimited";
        
    public string UploadSpeedFormatted => BandwidthUpKbps.HasValue 
        ? (BandwidthUpKbps.Value >= 1000 ? $"{BandwidthUpKbps.Value / 1000.0} Mbps" : $"{BandwidthUpKbps.Value} Kbps") 
        : "Unlimited";

    // Returns populated count only if requested by endpoints
    public int? ActiveSubscribersCount { get; set; }
}
