using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Plans.Core.Models;

[Table("radius_packages")]
public class RadiusPackage
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("radius_group_id")]
    public int RadiusGroupId { get; set; }

    [Column("price_cents")]
    public int PriceCents { get; set; }

    [Column("billing_cycle")]
    public string BillingCycle { get; set; } = "monthly";

    [Column("bandwidth_up_kbps")]
    public int? BandwidthUpKbps { get; set; }

    [Column("bandwidth_down_kbps")]
    public int? BandwidthDownKbps { get; set; }

    [Column("session_timeout_seconds")]
    public int SessionTimeoutSeconds { get; set; } = 86400;

    [Column("idle_timeout_seconds")]
    public int IdleTimeoutSeconds { get; set; } = 600;

    [Column("max_devices")]
    public int MaxDevices { get; set; } = 1;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
