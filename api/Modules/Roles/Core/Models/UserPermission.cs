using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Roles.Core.Models;

[Table("user_permissions")]
public class UserPermission
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("permission_id")]
    public int PermissionId { get; set; }

    [Column("is_granted")]
    public bool IsGranted { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public int? CreatedBy { get; set; }
}
