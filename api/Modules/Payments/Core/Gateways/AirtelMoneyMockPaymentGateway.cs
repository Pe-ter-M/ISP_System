using InternetProvider.Api.Modules.Payments.Interfaces;
using InternetProvider.Api.Modules.Payments.Core;

namespace InternetProvider.Api.Modules.Payments.Core.Gateways;

public class AirtelMoneyMockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<AirtelMoneyMockPaymentGateway> _log;

    public AirtelMoneyMockPaymentGateway(ILogger<AirtelMoneyMockPaymentGateway> log)
    {
        _log = log;
    }

    public PaymentMethod Provider => PaymentMethod.Airtel;

    public async Task<PaymentResult> ProcessPaymentAsync(int amountCents, string phoneNumber, string reference)
    {
        _log.LogInformation("Airtel Money Payment Simulation initialized for {Phone} (Amount: {Amount} Kes)", phoneNumber, amountCents / 100.0);

        // Simulate network API delay
        await Task.Delay(1000);

        // Simulate decline for sandbox testing
        if (phoneNumber.StartsWith("999"))
        {
            _log.LogWarning("Airtel Money push rejected: Subscriber cellular line is suspended");
            return new PaymentResult(false, null, "Simulated Airtel reject: [Subscriber status invalid or suspended]");
        }

        // Generate a standard simulated Airtel receipt (e.g. AM7629.0910)
        var random = new Random();
        var airtelRef = $"AM{random.Next(1000, 9999)}.{random.Next(1000, 9999)}";

        _log.LogInformation("Airtel Money transaction SUCCESS. Receipt ID: {Receipt}", airtelRef);
        return new PaymentResult(true, airtelRef, null);
    }
}