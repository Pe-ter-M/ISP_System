using InternetProvider.Api.Modules.Plans.Core.Models;

namespace InternetProvider.Api.Modules.Plans.Interfaces;

public interface IPlanRepository
{
    Task<List<RadiusPackage>> GetAllActiveAsync();
    Task<RadiusPackage?> GetByIdAsync(int id);
    Task<RadiusPackage> CreateAsync(RadiusPackage plan);
    Task UpdateAsync(RadiusPackage plan);
    Task<bool> NameExistsAsync(string name);
    Task<string?> GetGroupNameAsync(int groupId);
    Task SyncGroupQosAsync(RadiusPackage plan);
}
