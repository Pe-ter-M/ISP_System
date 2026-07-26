using InternetProvider.Api.Modules.Nas.Dtos;
using InternetProvider.Api.Modules.Nas.Interfaces;
using InternetProvider.Api.Modules.Nas.Core.Models;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Nas.Core;

public class NasService : INasService
{
    private readonly INasRepository _repo;
    private readonly ILogger<NasService> _log;

    public NasService(INasRepository repo, ILogger<NasService> log)
    {
        _repo = repo;
        _log = log;
    }

    public async Task<PaginatedResponse<NasResponse>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false)
    {
        _log.LogDebug("Processing get NAS clients (page {Page}, size {PageSize})", page, pageSize);
        var result = await _repo.GetAllAsync(page, pageSize, search, sortBy, sortDesc);

        return new PaginatedResponse<NasResponse>
        {
            Items = result.Items.Select(MapToResponse).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<NasResponse> GetByIdAsync(int id)
    {
        _log.LogDebug("Processing get NAS client by ID {NasId}", id);
        var nasClient = await _repo.GetByIdAsync(id);

        if (nasClient == null)
        {   
            _log.LogWarning("NAS client with ID {NasId} not found", id);
            throw new NotFoundException($"NAS client not found");
        }

        return MapToResponse(nasClient);
    }

    public async Task<NasResponse> CreateAsync(CreateNasRequest request)
    {
        _log.LogDebug("Processing create NAS client request for {Nasname} ({Shortname})",
            request.Nasname, request.Shortname);

        // Server-side validation
        if (string.IsNullOrWhiteSpace(request.Nasname))
            throw new ConflictException("NAS name is required");
        if (string.IsNullOrWhiteSpace(request.Shortname))
            throw new ConflictException("Short name is required");
        if (string.IsNullOrWhiteSpace(request.Secret))
            throw new ConflictException("Secret is required");
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new ConflictException("Type is required");

        if (await _repo.NasnameExistsAsync(request.Nasname))
        {
            _log.LogWarning("Duplicate NAS name attempt: {Nasname}", request.Nasname);
            throw new ConflictException($"NAS name '{request.Nasname}' is already in use.");
        }

        var nasClient = new NasClient
        {
            Nasname = request.Nasname,
            Shortname = request.Shortname,
            Type = request.Type,
            Ports = request.Ports,
            Secret = request.Secret,
            Server = request.Server,
            Community = request.Community,
            Description = request.Description
        };

        var created = await _repo.CreateAsync(nasClient);
        return MapToResponse(created);
    }

    public async Task<NasResponse> UpdateAsync(int id, UpdateNasRequest request)
    {
        _log.LogDebug("Processing update NAS client request for ID {NasId}", id);

        var existingNasClient = await _repo.GetByIdAsync(id);
        if (existingNasClient == null)
        {
            throw new NotFoundException($"NAS client not found");
        }

        // Server-side validation
        if (string.IsNullOrWhiteSpace(request.Nasname))
            throw new ConflictException("NAS name is required");
        if (string.IsNullOrWhiteSpace(request.Shortname))
            throw new ConflictException("Short name is required");
        if (string.IsNullOrWhiteSpace(request.Secret))
            throw new ConflictException("Secret is required");
        if (string.IsNullOrWhiteSpace(request.Type))
            throw new ConflictException("Type is required");

        // Check if nasname is already used by another client
        if (await _repo.NasnameExistsAsync(request.Nasname, id))
        {
            _log.LogWarning("Duplicate NAS name attempt during update: {Nasname}", request.Nasname);
            throw new ConflictException($"NAS name '{request.Nasname}' is already in use by another client.");
        }

        existingNasClient.Nasname = request.Nasname;
        existingNasClient.Shortname = request.Shortname;
        existingNasClient.Type = request.Type;
        existingNasClient.Ports = request.Ports;
        existingNasClient.Secret = request.Secret;
        existingNasClient.Server = request.Server;
        existingNasClient.Community = request.Community;
        existingNasClient.Description = request.Description;

        var updated = await _repo.UpdateAsync(existingNasClient);
        return MapToResponse(updated);
    }

    public async Task DeleteAsync(int id)
    {
        _log.LogDebug("Processing delete NAS client request for ID {NasId}", id);

        var deleted = await _repo.DeleteAsync(id);
        if (!deleted)
        {
            throw new NotFoundException($"NAS client not found");
        }

    }

    private static NasResponse MapToResponse(NasClient nasClient)
    {
        return new NasResponse
        {
            Id = nasClient.Id,
            Nasname = nasClient.Nasname,
            Shortname = nasClient.Shortname,
            Type = nasClient.Type,
            Ports = nasClient.Ports,
            Server = nasClient.Server ?? string.Empty,
            Community = nasClient.Community,
            Description = nasClient.Description
        };
    }
}
