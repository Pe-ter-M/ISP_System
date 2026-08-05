using InternetProvider.Api.Modules.Payments.Interfaces;
using InternetProvider.Api.Modules.Payments.Core;

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

    public IPaymentGateway GetGateway(PaymentMethod provider)
    {
        _log.LogDebug("Resolving payment gateway for provider enum value: '{Provider}'", provider);
        
        var gateway = _gateways.FirstOrDefault(g => g.Provider == provider);
        if (gateway == null)
        {
            _log.LogError("Unsupported payment provider enum requested: '{Provider}'", provider);
            throw new NotSupportedException($"Payment provider '{provider}' is not supported in this environment configuration.");
        }

        _log.LogDebug("Successfully matched provider gateway: {Provider}", gateway.Provider);
        return gateway;
    }
}