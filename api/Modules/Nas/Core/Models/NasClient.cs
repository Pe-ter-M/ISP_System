using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Nas.Core.Models;

[Table("nas")]
public class NasClient
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nasname")]
    public string Nasname { get; set; } = string.Empty;

    [Column("shortname")]
    public string Shortname { get; set; } = string.Empty;

    [Column("type")]
    public string Type { get; set; } = "other";

    [Column("ports")]
    public int? Ports { get; set; }

    [Column("secret")]
    public string Secret { get; set; } = string.Empty;

    [Column("server")]
    public string? Server { get; set; }

    [Column("community")]
    public string? Community { get; set; }

    [Column("description")]
    public string? Description { get; set; }
}
