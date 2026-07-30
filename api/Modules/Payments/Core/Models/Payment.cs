using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InternetProvider.Api.Modules.Payments.Core.Models;

[Table("payments")]
public class Payment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("subscription_id")]
    public int SubscriptionId { get; set; }

    [Column("amount_cents")]
    public int AmountCents { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "KES";

    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "Mock"; // e.g. "Mpesa", "Airtel", "Mock"

    [Column("status")]
    public string Status { get; set; } = "Pending"; // "Pending", "Completed", "Failed"

    [Column("reference_number")]
    public string? ReferenceNumber { get; set; } // External network receipt/trans ID

    [Column("phone_number")]
    public string? PhoneNumber { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}