namespace InternetProvider.Api.Modules.Settings.Dtos;

public record CreateSettingRequest(string Key, string Value, string? Description);
public record UpdateSettingRequest(string Value, string? Description);
