using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using InternetProvider.Api.Modules.Users.Core.Models;

namespace InternetProvider.Api.Modules.Staff.Core.Models;

[Table("staff")]
public class Staff
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("staff_code")]
    public string StaffCode { get; set; } = string.Empty;

    [Column("salary_cents")]
    public long SalaryCents { get; set; }

    [Column("employment_type")]
    public string EmploymentType { get; set; } = "full-time";

    [Column("date_joined")]
    public DateTime? DateJoined { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}
