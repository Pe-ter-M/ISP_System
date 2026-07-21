using InternetProvider.Api.Modules.Settings.Dtos;

namespace InternetProvider.Api.Modules.Settings.Interfaces;

public interface ISettingService
{
    Task<SettingResponse?> GetByKeyAsync(string key);
    Task<SettingResponse> CreateAsync(CreateSettingRequest request, int? userId);
    Task<SettingResponse?> UpdateAsync(string key, UpdateSettingRequest request, int? userId);
    Task DeleteAsync(string key);
}
