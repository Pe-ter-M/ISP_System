using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Staff.Dtos;
using InternetProvider.Api.Modules.Staff.Interfaces;
using InternetProvider.Api.Modules.Infrastructure.Core;
using StaffEntity = InternetProvider.Api.Modules.Staff.Core.Models.Staff;

namespace InternetProvider.Api.Modules.Staff.Core;

public class StaffRepository : IStaffRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<StaffRepository> _log;

    public StaffRepository(AppDbContext db, ILogger<StaffRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<PaginatedResponse<StaffEntity>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc)
    {
        var query = _db.Staff
            .Include(s => s.User)
            .ThenInclude(u => u!.Role)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.User!.FullName.ToLower().Contains(term) ||
                s.StaffCode.ToLower().Contains(term) ||
                s.User!.Email.ToLower().Contains(term) ||
                (s.User!.Phone != null && s.User!.Phone.Contains(term)));
        }

        query = (sortBy?.ToLower()) switch
        {
            "name" => sortDesc ? query.OrderByDescending(s => s.User!.FullName) : query.OrderBy(s => s.User!.FullName),
            "code" => sortDesc ? query.OrderByDescending(s => s.StaffCode) : query.OrderBy(s => s.StaffCode),
            "email" => sortDesc ? query.OrderByDescending(s => s.User!.Email) : query.OrderBy(s => s.User!.Email),
            "role" => sortDesc ? query.OrderByDescending(s => s.User!.Role!.Name) : query.OrderBy(s => s.User!.Role!.Name),
            "salary" => sortDesc ? query.OrderByDescending(s => s.SalaryCents) : query.OrderBy(s => s.SalaryCents),
            "status" => sortDesc ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            "created" => sortDesc ? query.OrderByDescending(s => s.User!.CreatedAt) : query.OrderBy(s => s.User!.CreatedAt),
            _ => query.OrderBy(s => s.Id)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        _log.LogDebug("Fetched {Count}/{Total} staff members", items.Count, totalCount);
        return new PaginatedResponse<StaffEntity>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<StaffEntity?> GetByIdAsync(int id)
    {
        _log.LogDebug("Fetching staff member by ID {StaffId}", id);
        var staff = await _db.Staff
            .Include(s => s.User)
            .ThenInclude(u => u!.Role)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (staff == null)
            _log.LogWarning("Staff member with ID {StaffId} not found", id);

        return staff;
    }

    public async Task<string> GenerateStaffCodeAsync()
    {
        var last = await _db.Staff
            .OrderByDescending(s => s.Id)
            .Select(s => s.StaffCode)
            .FirstOrDefaultAsync();

        int num = 1;
        if (last != null && last.StartsWith("STF-"))
        {
            int.TryParse(last[4..], out num);
            num++;
        }
        return $"STF-{num:D4}";
    }

    public async Task<StaffEntity> CreateAsync(StaffEntity staff)
    {
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();
        _log.LogInformation("Staff member created with ID {StaffId}: {Code}", staff.Id, staff.StaffCode);
        return staff;
    }

    public async Task<StaffEntity> UpdateAsync(StaffEntity staff)
    {
        _db.Staff.Update(staff);
        await _db.SaveChangesAsync();
        _log.LogInformation("Staff member {StaffId} updated", staff.Id);
        return staff;
    }

    public async Task<bool> IsPhoneTakenAsync(string phone)
    {
        return await _db.Users.AnyAsync(u => u.Phone == phone);
    }

    public async Task<StaffStatsResponse> GetStatsAsync()
    {
        _log.LogDebug("Computing staff stats");
        var stats = await _db.Staff
            .GroupBy(s => 1)
            .Select(g => new StaffStatsResponse
            {
                Total = g.Count(),
                Active = g.Count(s => s.Status == "active"),
                Inactive = g.Count(s => s.Status != "active"),
                TotalMonthlySalaryCents = g.Sum(s => s.SalaryCents),
            })
            .FirstOrDefaultAsync();

        return stats ?? new StaffStatsResponse();
    }
}
