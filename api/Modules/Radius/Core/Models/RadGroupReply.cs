using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Radius.Core.Models;

[Table("radgroupreply")]
public class RadGroupReply
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("groupname")]
    public string GroupName { get; set; } = string.Empty;

    [Column("attribute")]
    public string Attribute { get; set; } = string.Empty;

    [Column("op")]
    public string Op { get; set; } = "=";

    [Column("value")]
    public string Value { get; set; } = string.Empty;
}
