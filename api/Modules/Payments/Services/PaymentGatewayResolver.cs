using System.Text.Json;
using InternetProvider.Api.Services;
using InternetProvider.Api.Modules.Payments.Interfaces;
using InternetProvider.Api.Modules.Payments.Core;
using InternetProvider.Api.Modules.Settings.Interfaces;

namespace InternetProvider.Api.Modules.Payments.Services;

public class PaymentGatewayResolver
{
    private readonly IEnumerable<IPaymentGateway> _gateways;
    private readonly ISettingRepository _settingsRepo;
    private readonly ILogger<PaymentGatewayResolver> _log;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> gateways, ISettingRepository settingsRepo, ILogger<PaymentGatewayResolver> log)
    {
        _gateways = gateways;
        _settingsRepo = settingsRepo;
        _log = log;
    }

    public async Task<IPaymentGateway> GetGatewayAsync(PaymentMethod provider)
    {
        _log.LogDebug("Resolving payment gateway for provider enum value: '{Provider}'", provider);

        var gateway = _gateways.FirstOrDefault(g => g.Provider == provider);
        if (gateway == null)
        {
            _log.LogError("Unsupported payment provider enum requested: '{Provider}'", provider);
            throw new NotSupportedException($"Payment provider '{provider}' is not supported in this environment configuration.");
        }

        if (!await IsEnabledAsync(provider))
            throw new BadRequestException($"Payment method '{provider}' is currently disabled in settings.");

        _log.LogDebug("Successfully matched provider gateway: {Provider}", gateway.Provider);
        return gateway;
    }

    /// <summary>List the payment providers that are registered AND enabled in settings.</summary>
    public async Task<IEnumerable<PaymentMethod>> GetAvailableMethodsAsync()
    {
        var methods = _gateways.Select(g => g.Provider).Distinct().ToList();
        var enabled = await GetEnabledListAsync();
        if (enabled.Count > 0)
        {
            methods = methods
                .Where(m => enabled.Contains(m.ToString(), StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        _log.LogDebug("Exposing {Count} available payment methods", methods.Count);
        return methods;
    }

    /// <summary>When the `payment_methods` setting is set, only those providers are active.</summary>
    private async Task<bool> IsEnabledAsync(PaymentMethod provider)
    {
        var enabled = await GetEnabledListAsync();
        return enabled.Count == 0 || enabled.Contains(provider.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<List<string>> GetEnabledListAsync()
    {
        var setting = await _settingsRepo.GetByKeyAsync("payment_methods");
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value)) return new List<string>();
        return ParseEnabledList(setting.Value);
    }

    /// <summary>Accept either a JSON array (["Mpesa","Airtel"]) or a comma-separated list.</summary>
    private static List<string> ParseEnabledList(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(raw) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
