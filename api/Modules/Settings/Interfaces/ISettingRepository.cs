using InternetProvider.Api.Modules.Settings.Core.Models;

namespace InternetProvider.Api.Modules.Settings.Interfaces;

public interface ISettingRepository
{
    Task<Setting?> GetByKeyAsync(string key);
    Task<Setting> CreateAsync(Setting setting);
    Task<Setting?> UpdateAsync(string key, string value, string? description, int? updatedBy);
    Task<bool> DeleteAsync(string key);
    Task<bool> ExistsAsync(string key);
}
