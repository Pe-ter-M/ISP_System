369369using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Customers.Core.Models;

[Table("customers")]
public class Customer
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("customer_code")]
    public string CustomerCode { get; set; } = string.Empty;

    [Column("business_name")]
    public string? BusinessName { get; set; }

    [Column("customer_type")]
    public string CustomerType { get; set; } = "residential";

    [Column("phone")]
    public string Phone { get; set; } = string.Empty;

    [Column("service_address")]
    public string? ServiceAddress { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("region")]
    public string? Region { get; set; }

    [Column("gps_lat")]
    public double? GpsLat { get; set; }

    [Column("gps_lng")]
    public double? GpsLng { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
