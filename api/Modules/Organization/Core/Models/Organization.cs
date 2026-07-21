using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Organization.Core.Models;

[Table("organization")]
public class Organization
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("short_name")]
    public string? ShortName { get; set; }

    [Column("tagline")]
    public string? Tagline { get; set; }

    [Column("logo_url")]
    public string? LogoUrl { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "KSH";

    [Column("currency_symbol")]
    public string CurrencySymbol { get; set; } = "KSh";

    [Column("timezone")]
    public string Timezone { get; set; } = "Africa/Nairobi";

    [Column("support_email")]
    public string? SupportEmail { get; set; }

    [Column("support_phone")]
    public string? SupportPhone { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("setup_completed")]
    public bool SetupCompleted { get; set; }

    [Column("setup_completed_at")]
    public DateTime? SetupCompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
