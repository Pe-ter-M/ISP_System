using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.RadPostAuth.Core.Models;

[Table("radpostauth")]
public class RadPostAuth
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("pass")]
    public string? Pass { get; set; }

    [Column("reply")]
    public string? Reply { get; set; }

    [Column("CalledStationId")]
    public string? CalledStationId { get; set; }

    [Column("CallingStationId")]
    public string? CallingStationId { get; set; }

    [Column("authdate")]
    public DateTime AuthDate { get; set; }

    [Column("Class")]
    public string? Class { get; set; }
}
