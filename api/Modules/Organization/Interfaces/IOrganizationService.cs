using InternetProvider.Api.Modules.Organization.Dtos;

namespace InternetProvider.Api.Modules.Organization.Interfaces;

public interface IOrganizationService
{
    Task<OrganizationResponse?> GetAsync();
    Task<OrganizationResponse?> UpdateAsync(UpdateOrganizationRequest request);
}
