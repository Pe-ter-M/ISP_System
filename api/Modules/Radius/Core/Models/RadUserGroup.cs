using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Radius.Core.Models;

[Table("radusergroup")]
public class RadUserGroup
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    public string UserName { get; set; } = string.Empty;

    [Column("groupname")]
    public string GroupName { get; set; } = string.Empty;

    [Column("priority")]
    public int Priority { get; set; } = 1;
}
