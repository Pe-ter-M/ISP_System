using InternetProvider.Api.Modules.Organization.Dtos;
using InternetProvider.Api.Modules.Organization.Interfaces;

namespace InternetProvider.Api.Modules.Organization.Core;

public class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository _repo;
    private readonly ILogger<OrganizationService> _log;

    public OrganizationService(IOrganizationRepository repo, ILogger<OrganizationService> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<OrganizationResponse?> GetAsync()
    {
        _log.LogDebug("Getting organization");
        var org = await _repo.GetAsync();
        return org == null ? null : MapToResponse(org);
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
