using InternetProvider.Api.Modules.Plans.Core.Models;

namespace InternetProvider.Api.Modules.Plans.Interfaces;

public interface IPlanRepository
{
    Task<List<RadiusPackage>> GetAllActiveAsync();
    Task<RadiusPackage?> GetByIdAsync(int id);
    Task<RadiusPackage> CreateAsync(RadiusPackage plan);
    Task<bool> NameExistsAsync(string name);
    Task<string?> GetGroupNameAsync(int groupId);
    Task SyncGroupPolicyAsync(RadiusPackage plan);
    Task<RadiusPackage> UpdatePlanWithPolicyAsync(RadiusPackage plan);
    Task DeleteAsync(RadiusPackage plan);
}
