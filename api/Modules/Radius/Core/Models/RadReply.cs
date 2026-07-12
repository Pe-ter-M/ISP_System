using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Radius.Core.Models;

[Table("radreply")]
public class RadReply
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("UserName")]
    public string UserName { get; set; } = string.Empty;

    [Column("Attribute")]
    public string Attribute { get; set; } = string.Empty;

    [Column("op")]
    public string Op { get; set; } = "=";

    [Column("Value")]
    public string Value { get; set; } = string.Empty;
}
