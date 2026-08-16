namespace InternetProvider.Api.Modules.Nas.Dtos;

public class NasResponse
{
    public int Id { get; set; }
    public string Nasname { get; set; } = string.Empty;
    public string Shortname { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? Ports { get; set; }
    public string Server { get; set; } = string.Empty;
    public string? Community { get; set; }
    public string? Description { get; set; }
}
