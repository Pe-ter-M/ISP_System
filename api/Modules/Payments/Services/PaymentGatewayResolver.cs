using InternetProvider.Api.Modules.Payments.Interfaces;

namespace InternetProvider.Api.Modules.Payments.Services;

public class PaymentGatewayResolver
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly ILogger<PaymentGatewayResolver> _log;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways, ILogger<PaymentGatewayResolver> log)
    {
        _gateways = gateways;
        _log = log;
    }

    public IPaymentGateway GetGateway(string provider)
    {
        _log.LogDebug("Resolving payment gateway for provider: '{Provider}'", provider);
        
        var gateway = _gateways.FirstOrDefault(g => g.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));
        if (gateway == null)
        {
            _log.LogError("Unsupported payment provider requested: '{Provider}'", provider);
            throw new NotSupportedException($"Payment provider '{provider}' is not supported in this environment configuration.");
        }

        _log.LogDebug("Successfully matched provider: {Provider}", gateway.ProviderName);
        return gateway;
    }
}