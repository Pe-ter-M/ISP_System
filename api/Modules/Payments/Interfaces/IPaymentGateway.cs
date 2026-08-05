using InternetProvider.Api.Modules.Payments.Core;

namespace InternetProvider.Api.Modules.Payments.Interfaces;

public interface IPaymentGateway
{
    PaymentMethod Provider { get; } // Enum constraint

    Task<PaymentResult> ProcessPaymentAsync(int amountCents, string phoneNumber, string reference);
}

public record PaymentResult(bool IsSuccess, string? ReferenceNumber, string? ErrorMessage);