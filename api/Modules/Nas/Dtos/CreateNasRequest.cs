namespace InternetProvider.Api.Modules.Nas.Dtos;

public record CreateNasRequest(
    string Nasname,
    string Shortname,
    string Type,
    int? Ports,
    string Secret,
    string? Server,
    string? Community,
    string? Description
);
