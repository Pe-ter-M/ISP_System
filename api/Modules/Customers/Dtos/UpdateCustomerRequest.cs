namespace InternetProvider.Api.Modules.Customers.Dtos;

public record UpdateCustomerRequest(
    string FullName,
    string Email,
    string? Phone,
    string? BusinessName,
    string? CustomerType,
    string? ServiceAddress,
    string? City,
    string? Region,
    double? GpsLat,
    double? GpsLng,
    string? Status,
    string? Notes
);
