using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Nas.Core.Models;
using InternetProvider.Api.Modules.Nas.Interfaces;
using InternetProvider.Api.Modules.Nas.Dtos;
using InternetProvider.Api.Modules.Infrastructure.Core;

namespace InternetProvider.Api.Modules.Nas.Core;

public class NasRepository : INasRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<NasRepository> _log;

    public NasRepository(AppDbContext db, ILogger<NasRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<NasClient?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching NAS client by ID {NasId}", id);
        var nasClient = await _db.NasClients.FirstOrDefaultAsync(n => n.Id == id);

        if (nasClient == null)
            _log.LogWarning("NAS client with ID {NasId} not found", id);
        else
            _log.LogDebug("Found NAS client {NasId}: {Nasname}", id, nasClient.Nasname);

        return nasClient;
    }

    public async Task<PaginatedResponse<NasClient>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, string? sortBy = null, bool sortDesc = false)
    {
        _log.LogDebug("Fetching NAS clients (page {Page}, size {PageSize}, search '{Search}', sort '{SortBy}', desc {SortDesc})",
            page, pageSize, search, sortBy, sortDesc);
        var query = _db.NasClients.AsNoTracking().AsQueryable();

        // ── Search ──
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(n =>
                n.Nasname.ToLower().Contains(term) ||
                n.Shortname.ToLower().Contains(term) ||
                (n.Description != null && n.Description.ToLower().Contains(term)));
        }

        // ── Sort ──
        query = (sortBy?.ToLower()) switch
        {
            "nasname" => sortDesc ? query.OrderByDescending(n => n.Nasname) : query.OrderBy(n => n.Nasname),
            "shortname" => sortDesc ? query.OrderByDescending(n => n.Shortname) : query.OrderBy(n => n.Shortname),
            "type" => sortDesc ? query.OrderByDescending(n => n.Type) : query.OrderBy(n => n.Type),
            "server" => sortDesc ? query.OrderByDescending(n => n.Server) : query.OrderBy(n => n.Server),
            _ => query.OrderBy(n => n.Id) // default
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        _log.LogDebug("Fetched {Count}/{Total} NAS clients (page {Page}, size {PageSize})", items.Count, totalCount, page, pageSize);
        return new PaginatedResponse<NasClient>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<NasClient> CreateAsync(NasClient nasClient)
    {
        _log.LogDebug("Creating NAS client {Nasname} ({Shortname})", nasClient.Nasname, nasClient.Shortname);
        _db.NasClients.Add(nasClient);
        await _db.SaveChangesAsync();
        _log.LogDebug("NAS client created with ID {NasId}", nasClient.Id);

        return (await GetByIdAsync(nasClient.Id))!;
    }

    public async Task<NasClient> UpdateAsync(NasClient nasClient)
    {
        _log.LogDebug("Updating NAS client {NasId}: {Nasname}", nasClient.Id, nasClient.Nasname);
        _db.NasClients.Update(nasClient);
        await _db.SaveChangesAsync();
        _log.LogDebug("NAS client {NasId} updated successfully", nasClient.Id);

        return (await GetByIdAsync(nasClient.Id))!;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        _log.LogDebug("Deleting NAS client {NasId}", id);
        var nasClient = await _db.NasClients.FirstOrDefaultAsync(n => n.Id == id);
        
        if (nasClient == null)
        {
            _log.LogWarning("NAS client with ID {NasId} not found for deletion", id);
            return false;
        }

        _db.NasClients.Remove(nasClient);
        await _db.SaveChangesAsync();
        _log.LogDebug("NAS client {NasId} deleted successfully", id);
        return true;
    }

    public async Task<bool> NasnameExistsAsync(string nasname, int? excludeId = null)
    {
        _log.LogDebug("Checking existence of NAS name {Nasname} (excluding ID {ExcludeId})", nasname, excludeId);
        var query = _db.NasClients.Where(n => n.Nasname == nasname);
        
        if (excludeId.HasValue)
            query = query.Where(n => n.Id != excludeId.Value);
        
        var exists = await query.AnyAsync();
        _log.LogDebug("Nasname {Nasname} exists (excluding ID {ExcludeId}): {Exists}", nasname, excludeId, exists);
        return exists;
    }
}
