using InternetProvider.Api.Modules.Payments.Interfaces;
using InternetProvider.Api.Modules.Payments.Core;

namespace InternetProvider.Api.Modules.Payments.Core.Gateways;

public class MpesaMockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MpesaMockPaymentGateway> _log;

    public MpesaMockPaymentGateway(ILogger<MpesaMockPaymentGateway> log)
    {
        _log = log;
    }

    public PaymentMethod Provider => PaymentMethod.Mpesa;

    public async Task<PaymentResult> ProcessPaymentAsync(int amountCents, string phoneNumber, string reference)
    {
        _log.LogInformation("M-Pesa STK Push Simulator triggered for {Phone} (Amount: {Amount} Kes)", phoneNumber, amountCents / 100.0);

        // 1. Simulate the time it takes for a user to see the STK prompt, type their PIN, and respond (e.g., 1.5 seconds)
        await Task.Delay(1500);

        // 2. Add realistic validation rules for simulation & testing
        if (phoneNumber.StartsWith("000") || phoneNumber.EndsWith("000"))
        {
            _log.LogWarning("M-Pesa push failed: Simulated insufficient wallet balance or timeout on phone {Phone}", phoneNumber);
            return new PaymentResult(false, null, "Simulated M-Pesa reject: [Request cancelled by user or Insufficient Funds]");
        }

        // 3. Generate a realistic M-Pesa Transaction ID (e.g., QG719AX40P: 10 chars, uppercase alpha-numeric, ending in letter)
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var numbers = "0123456789";
        var random = new Random();

        var mpesaRef = string.Concat(
            alphabet[random.Next(26)],
            alphabet[random.Next(26)],
            numbers[random.Next(10)],
            numbers[random.Next(10)],
            alphabet[random.Next(26)],
            alphabet[random.Next(26)],
            numbers[random.Next(10)],
            numbers[random.Next(10)],
            alphabet[random.Next(26)],
            alphabet[random.Next(26)]
        );

        _log.LogInformation("M-Pesa payment simulated successfully. M-Pesa Receipt ID: {Receipt}", mpesaRef);
        return new PaymentResult(true, mpesaRef, null);
    }
}