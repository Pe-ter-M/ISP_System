using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Radius.Core.Models;

[Table("radgroupcheck")]
public class RadGroupCheck
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("GroupName")]
    public string GroupName { get; set; } = string.Empty;

    [Column("Attribute")]
    public string Attribute { get; set; } = string.Empty;

    [Column("op")]
    public string Op { get; set; } = "==";

    [Column("Value")]
    public string Value { get; set; } = string.Empty;
}
