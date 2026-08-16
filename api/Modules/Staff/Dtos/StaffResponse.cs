namespace InternetProvider.Api.Modules.Staff.Dtos;

public class StaffSummaryResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string StaffCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public long SalaryCents { get; set; }
    public string EmploymentType { get; set; } = "full-time";
    public DateTime? DateJoined { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "active";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Friendly presentation property (KES formatting)
    public double Salary => SalaryCents / 100.0;
}

public record CreateStaffRequest(
    string Email,
    string Password,
    string FullName,
    string Phone,
    int RoleId,
    long? SalaryCents,
    string? EmploymentType,
    DateTime? DateJoined,
    string? Notes
);

public record UpdateStaffRequest(
    string? FullName,
    string? Email,
    string? Phone,
    int? RoleId,
    long? SalaryCents,
    string? EmploymentType,
    DateTime? DateJoined,
    string? Notes,
    string? Status
);

public class StaffStatsResponse
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Inactive { get; set; }
    public long TotalMonthlySalaryCents { get; set; }
    public double TotalMonthlySalary => TotalMonthlySalaryCents / 100.0;
}

public record DeleteStaffResult(bool HardDeleted, string Message);
