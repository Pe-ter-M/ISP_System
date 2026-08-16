using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Audit.Core.Models;

/// <summary>
/// A centralized, system-wide audit entry. Records a SUCCESSFUL create/update/delete
/// (or a login attempt) across the application, capturing who (actor), what (entity,
/// action, readable summary), when (created_at date+time), and supporting detail
/// (compact old-&gt;new field changes, IP, HTTP method/path).
///
/// Storage is intentionally compact: actor display details and the current entity
/// label are resolved lazily at read time (via the repository) rather than
/// denormalized into every row.
/// </summary>
[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>User.Id of the person who performed the action, if authenticated.</summary>
    [Column("actor_user_id")]
    public int? ActorUserId { get; set; }

    /// <summary>"staff" | "customer" | "system" | "anonymous".</summary>
    [Column("actor_type")]
    public string ActorType { get; set; } = "system";

    /// <summary>Logical entity type, e.g. "customer", "staff", "user", "role", "plan", "nas", "setting", "organization".</summary>
    [Column("entity_type")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Id of the affected record, when applicable.</summary>
    [Column("entity_id")]
    public int? EntityId { get; set; }

    /// <summary>"create" | "update" | "delete" | "login_success" | "login_failure".</summary>
    [Column("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>Human-readable summary of what changed, e.g. "Plan 'Standard 30Mbs' updated".</summary>
    [Column("summary")]
    public string Summary { get; set; } = string.Empty;

    /// <summary>Compact JSON of old-&gt;new field changes (nullable). Filled in at read time.</summary>
    [Column("changes", TypeName = "jsonb")]
    public string? Changes { get; set; }

    [Column("http_method")]
    public string? HttpMethod { get; set; }

    [Column("http_path")]
    public string? HttpPath { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    /// <summary>Date AND time the audit entry was recorded.</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
