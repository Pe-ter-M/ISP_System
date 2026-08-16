namespace InternetProvider.Api.Modules.Payments.Dtos;

public record PaymentResponse(
    int Id,
    int SubscriptionId,
    int AmountCents,
    string Currency,
    string PaymentMethod,
    string Status,
    string? ReferenceNumber,
    string? PhoneNumber,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
