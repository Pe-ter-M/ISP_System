namespace InternetProvider.Api.Modules.Payments.Interfaces;

public interface IPaymentGateway
{
    string ProviderName { get; } // "Mock", "Mpesa", "Airtel" etc.

    Task<PaymentResult> ProcessPaymentAsync(int amountCents, string phoneNumber, string reference);
}

public record PaymentResult(bool IsSuccess, string? ReferenceNumber, string? ErrorMessage);