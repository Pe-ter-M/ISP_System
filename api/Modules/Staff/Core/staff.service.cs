using InternetProvider.Api.Common;
using InternetProvider.Api.Modules.Staff.Dtos;
using InternetProvider.Api.Modules.Staff.Interfaces;
using Microsoft.EntityFrameworkCore;
using InternetProvider.Api.Modules.Users.Core.Models;
using InternetProvider.Api.Modules.Roles.Core.Models;
using InternetProvider.Api.Modules.Infrastructure.Core;
using InternetProvider.Api.Services;

namespace InternetProvider.Api.Modules.Staff.Core;

public class StaffService : IStaffService
{
    private readonly IStaffRepository _repo;
    private readonly ILogger<StaffService> _log;
    private readonly AppDbContext _db;

    public StaffService(IStaffRepository repo, ILogger<StaffService> log, AppDbContext db)
    {
        _repo = repo;
        _log = log;
        _db = db;
    }

    public async Task<PaginatedResponse<StaffSummaryResponse>> GetAllAsync(int page, int pageSize, string? search, string? sortBy, bool sortDesc)
    {
        _log.LogDebug("Getting staff page {Page} size {PageSize}", page, pageSize);
        var result = await _repo.GetAllAsync(page, pageSize, search, sortBy, sortDesc);

        return new PaginatedResponse<StaffSummaryResponse>
        {
            Items = result.Items.Select(MapSummary).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
    }

    public async Task<StaffSummaryResponse> GetByIdAsync(int id)
    {
        _log.LogDebug("Getting staff detail for ID {StaffId}", id);
        var staff = await _repo.GetByIdAsync(id);
        if (staff == null)
            throw new NotFoundException($"Staff member with ID {id} not found");

        return MapSummary(staff);
    }

    public async Task<StaffSummaryResponse> CreateAsync(CreateStaffRequest request)
    {
        _log.LogInformation("Creating staff member: {FullName}", request.FullName);

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ConflictException("Email is required");
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 4)
            throw new ConflictException("Password must be at least 4 characters");
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ConflictException("Full name is required");
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ConflictException("Phone number is required");
        if (request.RoleId <= 0)
            throw new ConflictException("A staff role is required");
        if (request.SalaryCents is < 0)
            throw new ConflictException("Salary cannot be negative");

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException($"Email '{request.Email}' is already in use");
        if (await _repo.IsPhoneTakenAsync(request.Phone))
            throw new ConflictException($"Phone '{request.Phone}' is already in use");

        var role = await _db.Roles.FindAsync(request.RoleId);
        if (role == null)
            throw new ConflictException($"Role with ID {request.RoleId} not found");
        if (role.Name.Equals(RoleNames.Of(SystemRole.Customer), StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Staff accounts must use a staff role, not the Customer role");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Phone = request.Phone,
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(); // flush to get user.Id; not committed yet

        var code = await _repo.GenerateStaffCodeAsync();
        var now = DateTime.UtcNow;
        // A bare date from the client (e.g. "2026-08-01") deserializes with Kind=Unspecified,
        // which Npgsql rejects for timestamptz — normalize to UTC first.
        var dateJoined = request.DateJoined.HasValue
            ? DateTime.SpecifyKind(request.DateJoined.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        var staff = new Models.Staff
        {
            UserId = user.Id,
            StaffCode = code,
            SalaryCents = request.SalaryCents ?? 0,
            EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? "full-time" : request.EmploymentType,
            DateJoined = dateJoined,
            Notes = request.Notes,
            Status = "active",
            UpdatedAt = now,
        };
        _db.Staff.Add(staff);
        await _db.SaveChangesAsync();

        await tx.CommitAsync();
        _log.LogInformation("Staff member {StaffId} ({Code}) created successfully", staff.Id, code);
        return MapSummary(staff);
    }

    public async Task<StaffSummaryResponse> UpdateAsync(int id, UpdateStaffRequest request)
    {
        _log.LogDebug("Processing update staff request for ID {StaffId}", id);

        var staff = await _repo.GetByIdAsync(id);
        if (staff == null)
            throw new NotFoundException($"Staff member with ID {id} not found");
        var user = staff.User;
        if (user == null)
            throw new NotFoundException($"User account for staff member {id} not found");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new ConflictException("Full name is required");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ConflictException("Email is required");
        if (string.IsNullOrWhiteSpace(request.Phone))
            throw new ConflictException("Phone number is required");

        if (user.Email != request.Email && await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new ConflictException($"Email '{request.Email}' is already in use");
        if (user.Phone != request.Phone && await _repo.IsPhoneTakenAsync(request.Phone))
            throw new ConflictException($"Phone '{request.Phone}' is already in use");

        if (request.RoleId.HasValue && request.RoleId.Value != user.RoleId)
        {
            var role = await _db.Roles.FindAsync(request.RoleId.Value);
            if (role == null)
                throw new ConflictException($"Role with ID {request.RoleId} not found");
            if (role.Name.Equals(RoleNames.Of(SystemRole.Customer), StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Staff accounts must use a staff role, not the Customer role");
            user.RoleId = role.Id;
        }

        var status = request.Status ?? staff.Status;
        if (status is not ("active" or "inactive"))
            throw new ConflictException("Invalid staff status choice. Choose 'active' or 'inactive'.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.IsActive = status == "active";
        user.UpdatedAt = DateTime.UtcNow;

        staff.SalaryCents = request.SalaryCents ?? staff.SalaryCents;
        staff.EmploymentType = string.IsNullOrWhiteSpace(request.EmploymentType) ? "full-time" : request.EmploymentType;
        staff.DateJoined = request.DateJoined.HasValue
            ? DateTime.SpecifyKind(request.DateJoined.Value, DateTimeKind.Utc)
            : staff.DateJoined;
        staff.Notes = request.Notes;
        staff.Status = status;
        staff.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(staff);
        await _db.SaveChangesAsync(); // persist the tracked user changes too

        _log.LogInformation("Staff member {StaffId} updated successfully", id);
        return MapSummary(staff);
    }

    public async Task<DeleteStaffResult> DeleteAsync(int id)
    {
        _log.LogDebug("Processing delete staff request for ID {StaffId}", id);

        var staff = await _repo.GetByIdAsync(id);
        if (staff == null)
            throw new NotFoundException($"Staff member with ID {id} not found");

        // Staff users have no dependent business records — hard delete the linked user too
        if (staff.User != null)
        {
            var overrides = await _db.UserPermissions.Where(up => up.UserId == staff.UserId).ToListAsync();
            _db.UserPermissions.RemoveRange(overrides);
        }
        _db.Staff.Remove(staff); // remove dependent first — User→Staff FK is required (no cascade)
        if (staff.User != null)
            _db.Users.Remove(staff.User);
        await _db.SaveChangesAsync();
        _log.LogInformation("Staff member {StaffId} and linked user account hard deleted", id);
        return new DeleteStaffResult(true, "Staff member deleted successfully.");
    }

    public async Task<StaffStatsResponse> GetStatsAsync()
    {
        _log.LogDebug("Processing staff stats request");
        return await _repo.GetStatsAsync();
    }

    private static StaffSummaryResponse MapSummary(Models.Staff s)
    {
        return new StaffSummaryResponse
        {
            Id = s.Id,
            UserId = s.UserId,
            StaffCode = s.StaffCode,
            FullName = s.User!.FullName,
            Email = s.User!.Email,
            Phone = s.User!.Phone ?? string.Empty,
            RoleId = s.User!.RoleId,
            RoleName = s.User!.Role?.Name ?? "Unknown",
            SalaryCents = s.SalaryCents,
            EmploymentType = s.EmploymentType,
            DateJoined = s.DateJoined ?? s.User!.CreatedAt,
            Notes = s.Notes,
            Status = s.Status,
            IsActive = s.User!.IsActive,
            CreatedAt = s.User!.CreatedAt,
            UpdatedAt = s.UpdatedAt,
        };
    }
}
