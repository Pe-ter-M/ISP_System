using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Plans.Core.Models;

[Table("radius_groups")]
public class RadiusGroup
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("group_name")]
    public string GroupName { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
