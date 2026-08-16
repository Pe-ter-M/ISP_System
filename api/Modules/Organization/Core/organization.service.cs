using InternetProvider.Api.Modules.Organization.Dtos;
using InternetProvider.Api.Modules.Organization.Interfaces;
using InternetProvider.Api.Modules.Settings.Interfaces;

namespace InternetProvider.Api.Modules.Organization.Core;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repo;
    private readonly ISettingRepository _settingsRepo;
    private readonly ILogger<OrganizationService> _log;

    public OrganizationService(IOrganizationRepository repo, ISettingRepository settingsRepo, ILogger<OrganizationService> log)
    {
        _repo = repo;
        _settingsRepo = settingsRepo;
        _log = log;
    }

    public async Task<OrganizationResponse?> GetAsync()
    {
        _log.LogDebug("Getting organization");
        var org = await _repo.GetAsync();
        if (org == null) return null;

        var response = MapToResponse(org);

        // Branding overrides from the dynamic settings table — a different ISP can
        // set its own name/contact/currency in Settings without code changes.
        async Task<string?> GetSettingValueAsync(string key)
        {
            var s = await _settingsRepo.GetByKeyAsync(key);
            return string.IsNullOrWhiteSpace(s?.Value) ? null : s.Value.Trim();
        }

        response.Name = await GetSettingValueAsync("company_name") ?? response.Name;
        response.ShortName = await GetSettingValueAsync("company_short_name") ?? response.ShortName;
        response.SupportPhone = await GetSettingValueAsync("company_phone") ?? response.SupportPhone;
        response.SupportEmail = await GetSettingValueAsync("company_email") ?? response.SupportEmail;
        response.Address = await GetSettingValueAsync("company_address") ?? response.Address;

        var currency = await GetSettingValueAsync("currency");
        if (currency != null)
        {
            response.Currency = currency;
            response.CurrencySymbol = currency.ToUpperInvariant() switch
            {
                "KES" or "KSH" => "KSh",
                "USD" => "$",
                "EUR" => "€",
                "GBP" => "£",
                _ => response.CurrencySymbol
            };
        }

        return response;
    }

    public async Task<OrganizationResponse?> UpdateAsync(UpdateOrganizationRequest request)
    {
        _log.LogInformation("Updating organization settings");
        var org = await _repo.GetAsync();
        if (org == null) return null;

        if (request.Name != null) org.Name = request.Name;
        if (request.ShortName != null) org.ShortName = request.ShortName;
        if (request.Tagline != null) org.Tagline = request.Tagline;
        if (request.Currency != null) org.Currency = request.Currency;
        if (request.CurrencySymbol != null) org.CurrencySymbol = request.CurrencySymbol;
        if (request.Timezone != null) org.Timezone = request.Timezone;
        if (request.SupportEmail != null) org.SupportEmail = request.SupportEmail;
        if (request.SupportPhone != null) org.SupportPhone = request.SupportPhone;
        if (request.Address != null) org.Address = request.Address;
        org.UpdatedAt = DateTime.UtcNow;

        var updated = await _repo.UpdateAsync(org);
        return MapToResponse(updated);
    }

    private static OrganizationResponse MapToResponse(Models.Organization org)
    {
        return new OrganizationResponse
        {
            Id = org.Id,
            Name = org.Name,
            ShortName = org.ShortName,
            Tagline = org.Tagline,
            LogoUrl = org.LogoUrl,
            Currency = org.Currency,
            CurrencySymbol = org.CurrencySymbol,
            Timezone = org.Timezone,
            SupportEmail = org.SupportEmail,
            SupportPhone = org.SupportPhone,
            Address = org.Address,
            SetupCompleted = org.SetupCompleted,
            CreatedAt = org.CreatedAt,
            UpdatedAt = org.UpdatedAt
        };
    }
}
