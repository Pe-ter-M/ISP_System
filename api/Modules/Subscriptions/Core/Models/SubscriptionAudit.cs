using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Subscriptions.Core.Models;

/// <summary>
/// Audit trail for a subscription — records who performed a change and what
/// changed (plan, period end, auto-renew, status). Staff updates are mandatory
/// to track so admins can see who modified a subscription and when.
/// </summary>
[Table("subscription_audits")]
public class SubscriptionAudit
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("subscription_id")]
    public int SubscriptionId { get; set; }

    /// <summary>User.Id of the staff member who made the change.</summary>
    [Column("changed_by")]
    public int? ChangedBy { get; set; }

    /// <summary>What changed, human-readable, e.g. "Plan changed from Basic 10 Mbps to Standard 20 Mbps".</summary>
    [Column("change")]
    public string Change { get; set; } = string.Empty;

    /// <summary>Optional brief field/notes for the change.</summary>
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
