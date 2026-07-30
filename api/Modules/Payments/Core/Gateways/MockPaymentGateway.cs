using InternetProvider.Api.Modules.Payments.Interfaces;

namespace InternetProvider.Api.Modules.Payments.Core.Gateways;

public class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _log;

    public MockPaymentGateway(ILogger<MockPaymentGateway> log)
    {
        _log = log;
    }

    public string ProviderName => "Mock";

    public async Task<PaymentResult> ProcessPaymentAsync(int amountCents, string phoneNumber, string reference)
    {
        _log.LogInformation("Processing simulated payment: {AmountCents} cents via {Phone}", amountCents, phoneNumber);
        
        // Simulate minor network API latency
        await Task.Delay(400);

        // Simulated check to test failure cases
        if (phoneNumber.Contains("000000000"))
        {
            _log.LogWarning("Mock transaction deliberately rejected by developer simulation rule");
            return new PaymentResult(false, null, "Simulated carrier timeout/declined balance");
        }

        var simulatedTransactionId = $"MOCK{Guid.NewGuid().ToString()[..8].ToUpper()}";
        _log.LogInformation("Simulated payment SUCCESS with receipt code {TxId}", simulatedTransactionId);
        
        return new PaymentResult(true, simulatedTransactionId, null);
    }
}