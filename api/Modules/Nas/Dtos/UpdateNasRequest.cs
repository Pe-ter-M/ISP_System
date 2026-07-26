namespace InternetProvider.Api.Modules.Nas.Dtos;

public record UpdateNasRequest(
    string Nasname,
    string Shortname,
    string Type,
    int? Ports,
    string Secret,
    string? Server,
    string? Community,
    string? Description
);
