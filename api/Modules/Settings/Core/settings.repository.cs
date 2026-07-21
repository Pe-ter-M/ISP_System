using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Settings.Core.Models;
using InternetProvider.Api.Modules.Settings.Interfaces;
using InternetProvider.Api.Modules.Infrastructure.Core;

namespace InternetProvider.Api.Modules.Settings.Core;

public class SettingRepository : ISettingRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<SettingRepository> _log;

    public SettingRepository(AppDbContext db, ILogger<SettingRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<Setting?> GetByKeyAsync(string key)
    {
        _log.LogDebug("Fetching setting {Key}", key);
        return await _db.Settings.FindAsync(key);
    }

    public async Task<Setting> CreateAsync(Setting setting)
    {
        _log.LogInformation("Creating setting {Key} = {Value}", setting.Key, setting.Value);
        _db.Settings.Add(setting);
        await _db.SaveChangesAsync();
        _log.LogInformation("Setting {Key} created", setting.Key);
        return setting;
    }

    public async Task<Setting?> UpdateAsync(string key, string value, string? description, int? updatedBy)
    {
        _log.LogInformation("Updating setting {Key}", key);
        var setting = await _db.Settings.FindAsync(key);
        if (setting == null) return null;

        setting.Value = value;
        if (description != null) setting.Description = description;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = updatedBy;

        await _db.SaveChangesAsync();
        _log.LogInformation("Setting {Key} updated", key);
        return setting;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        _log.LogInformation("Deleting setting {Key}", key);
        var setting = await _db.Settings.FindAsync(key);
        if (setting == null) return false;

        _db.Settings.Remove(setting);
        await _db.SaveChangesAsync();
        _log.LogInformation("Setting {Key} deleted", key);
        return true;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.Settings.AnyAsync(s => s.Key == key);
    }
}
