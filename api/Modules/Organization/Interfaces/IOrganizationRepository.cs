using InternetProviderOrg = InternetProvider.Api.Modules.Organization.Core.Models.Organization;

namespace InternetProvider.Api.Modules.Organization.Interfaces;

public interface IOrganizationRepository
{
    Task<InternetProviderOrg?> GetAsync();
    Task<InternetProviderOrg> UpdateAsync(InternetProviderOrg org);
}
