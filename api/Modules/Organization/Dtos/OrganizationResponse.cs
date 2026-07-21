namespace InternetProvider.Api.Modules.Organization.Dtos;

public class OrganizationResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Tagline { get; set; }
    public string? LogoUrl { get; set; }
    public string Currency { get; set; } = "KSH";
    public string CurrencySymbol { get; set; } = "KSh";
    public string Timezone { get; set; } = "Africa/Nairobi";
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public string? Address { get; set; }
    public bool SetupCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
