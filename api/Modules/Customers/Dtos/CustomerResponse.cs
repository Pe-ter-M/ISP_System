namespace InternetProvider.Api.Modules.Customers.Dtos;

public class CustomerSummaryResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string CustomerType { get; set; } = "residential";
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Region { get; set; }
    public string UsernamePpoe { get; set; } = string.Empty;
    public string PasswordPpoe { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
}

public class CustomerDetailResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? BusinessName { get; set; }
    public string CustomerType { get; set; } = "residential";
    public string? Email { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? ServiceAddress { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public double? GpsLat { get; set; }
    public double? GpsLng { get; set; }
    public string UsernamePpoe { get; set; } = string.Empty;
    public string PasswordPpoe { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<CustomerSubscriptionDto> Subscriptions { get; set; } = new();
}

public class CustomerSubscriptionDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CurrentPeriodEnd { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public record CreateCustomerRequest(
    string Email,
    string Password,
    string FullName,
    string Phone,
    string? BusinessName,
    string? CustomerType,
    string? ServiceAddress,
    string? City,
    string? Region
);
