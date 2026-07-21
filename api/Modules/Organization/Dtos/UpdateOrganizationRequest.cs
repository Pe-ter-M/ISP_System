namespace InternetProvider.Api.Modules.Organization.Dtos;

public class UpdateOrganizationRequest
{
    public string? Name { get; set; }
    public string? ShortName { get; set; }
    public string? Tagline { get; set; }
    public string? Currency { get; set; }
    public string? CurrencySymbol { get; set; }
    public string? Timezone { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? Address { get; set; }
}
