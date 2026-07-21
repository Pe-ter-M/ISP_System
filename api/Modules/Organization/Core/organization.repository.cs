using Microsoft.EntityFrameworkCore;
using InternetProviderOrg = InternetProvider.Api.Modules.Organization.Core.Models.Organization;
using InternetProvider.Api.Modules.Organization.Interfaces;
using InternetProvider.Api.Modules.Infrastructure.Core;

namespace InternetProvider.Api.Modules.Organization.Core;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrganizationRepository> _log;

    public OrganizationRepository(AppDbContext db, ILogger<OrganizationRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<InternetProviderOrg?> GetAsync()
    {
        _log.LogDebug("Fetching organization");
        return await _db.Organizations.FirstOrDefaultAsync();
    }

    public async Task<InternetProviderOrg> UpdateAsync(InternetProviderOrg org)
    {
        _log.LogDebug("Updating organization settings");
        _db.Organizations.Update(org);
        await _db.SaveChangesAsync();
        _log.LogDebug("Organization updated");
        return org;
    }
}
