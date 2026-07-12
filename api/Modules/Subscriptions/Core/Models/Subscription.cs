using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Subscriptions.Core.Models;

[Table("subscriptions")]
public class Subscription
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("customer_id")]
    public int CustomerId { get; set; }

    [Column("package_id")]
    public int PackageId { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("password")]
    public string Password { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("current_period_start")]
    public DateTime CurrentPeriodStart { get; set; }

    [Column("current_period_end")]
    public DateTime CurrentPeriodEnd { get; set; }

    [Column("auto_renew")]
    public bool AutoRenew { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
