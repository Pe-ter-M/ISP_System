using InternetProvider.Api.Modules.Settings.Dtos;
using InternetProvider.Api.Modules.Settings.Interfaces;
using InternetProvider.Api.Modules.Settings.Core.Models;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Settings.Core;

public class SettingService : ISettingService
{
    private readonly ISettingRepository _repo;
    private readonly ILogger<SettingService> _log;

    public SettingService(ISettingRepository repo, ILogger<SettingService> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<SettingResponse?> GetByKeyAsync(string key)
    {
        _log.LogDebug("Getting setting {Key}", key);
        var setting = await _repo.GetByKeyAsync(key);
        return setting == null ? null : MapToResponse(setting);
    }

    public async Task<SettingResponse> CreateAsync(CreateSettingRequest request, int? userId)
    {
        _log.LogInformation("Creating setting {Key}", request.Key);

        if (await _repo.ExistsAsync(request.Key))
            throw new ConflictException($"Setting '{request.Key}' already exists");

        var setting = new Setting
        {
            Key = request.Key,
            Value = request.Value,
            Description = request.Description,
            UpdatedBy = userId,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(setting);
        return MapToResponse(created);
    }

    public async Task<SettingResponse?> UpdateAsync(string key, UpdateSettingRequest request, int? userId)
    {
        _log.LogInformation("Updating setting {Key}", key);
        var updated = await _repo.UpdateAsync(key, request.Value, request.Description, userId);
        return updated == null ? null : MapToResponse(updated);
    }

    public async Task DeleteAsync(string key)
    {
        _log.LogInformation("Deleting setting {Key}", key);
        var deleted = await _repo.DeleteAsync(key);
        if (!deleted)
            throw new NotFoundException($"Setting '{key}' not found");
    }

    private static SettingResponse MapToResponse(Setting setting)
    {
        return new SettingResponse
        {
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            UpdatedAt = setting.UpdatedAt
        };
    }
}
