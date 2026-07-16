using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Roles.Core.Models;

[Table("permissions")]
public class Permission
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = string.Empty;

    [Column("group")]
    public string Group { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }
}
