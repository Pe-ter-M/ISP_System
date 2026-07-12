using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.RadAcct.Core.Models;

[Table("radacct")]
public class RadAcct
{
    [Key]
    [Column("RadAcctId")]
    public long RadAcctId { get; set; }

    [Column("AcctSessionId")]
    public string AcctSessionId { get; set; } = string.Empty;

    [Column("AcctUniqueId")]
    public string AcctUniqueId { get; set; } = string.Empty;

    [Column("UserName")]
    public string? UserName { get; set; }

    [Column("Realm")]
    public string? Realm { get; set; }

    [Column("NASIPAddress")]
    public string? NasIpAddress { get; set; }

    [Column("NASPortId")]
    public string? NasPortId { get; set; }

    [Column("NASPortType")]
    public string? NasPortType { get; set; }

    [Column("AcctStartTime")]
    public DateTime? AcctStartTime { get; set; }

    [Column("AcctUpdateTime")]
    public DateTime? AcctUpdateTime { get; set; }

    [Column("AcctStopTime")]
    public DateTime? AcctStopTime { get; set; }

    [Column("AcctInterval")]
    public long? AcctInterval { get; set; }

    [Column("AcctSessionTime")]
    public long? AcctSessionTime { get; set; }

    [Column("AcctInputOctets")]
    public long? AcctInputOctets { get; set; }

    [Column("AcctOutputOctets")]
    public long? AcctOutputOctets { get; set; }

    [Column("CalledStationId")]
    public string? CalledStationId { get; set; }

    [Column("CallingStationId")]
    public string? CallingStationId { get; set; }

    [Column("AcctTerminateCause")]
    public string? AcctTerminateCause { get; set; }

    [Column("ServiceType")]
    public string? ServiceType { get; set; }

    [Column("FramedProtocol")]
    public string? FramedProtocol { get; set; }

    [Column("FramedIPAddress")]
    public string? FramedIpAddress { get; set; }

    [Column("FramedIPv6Address")]
    public string? FramedIpv6Address { get; set; }

    [Column("Class")]
    public string? Class { get; set; }
}
