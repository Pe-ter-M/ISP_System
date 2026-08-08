using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InternetProvider.Api.Modules.Users.Core.Models;

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

    [Column("username_ppoe")]
    public string UsernamePpoe { get; set; } = string.Empty;

    [Column("password_ppoe")]
    public string PasswordPpoe { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
