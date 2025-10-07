using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;

namespace ReviewHub.Infrastructure.Services;

public class LocationService : ILocationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LocationService> _logger;

    public LocationService(ApplicationDbContext context, ILogger<LocationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<int>> GetUserAccessibleLocationIdsAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.LocationAccesses)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null || user.OrganizationId == null)
        {
            _logger.LogWarning("User {UserId} not found or not assigned to organization", userId);
            return new List<int>();
        }

        var accessibleLocationIds = new HashSet<int>();

        foreach (var access in user.LocationAccesses.Where(a => a.OrganizationId == user.OrganizationId))
        {
            if (access.HasAllLocationsAccess)
            {
                // User has access to all locations in organization
                var allLocationIds = await _context.Locations
                    .Where(l => l.OrganizationId == access.OrganizationId && l.IsActive)
                    .Select(l => l.Id)
                    .ToListAsync();

                accessibleLocationIds.UnionWith(allLocationIds);
                break; // If has all access, no need to check other accesses
            }
            else if (access.LocationId.HasValue)
            {
                // Specific location access
                accessibleLocationIds.Add(access.LocationId.Value);
            }
            else if (access.LocationGroupId.HasValue)
            {
                // Group access - get all locations in group (recursive)
                var groupLocationIds = await GetLocationIdsInGroupRecursiveAsync(access.LocationGroupId.Value);
                accessibleLocationIds.UnionWith(groupLocationIds);
            }
        }

        return accessibleLocationIds.ToList();
    }

    public async Task<List<Location>> GetUserAccessibleLocationsAsync(int userId)
    {
        var locationIds = await GetUserAccessibleLocationIdsAsync(userId);

        return await _context.Locations
            .Where(l => locationIds.Contains(l.Id))
            .Include(l => l.LocationGroup)
            .Include(l => l.Manager)
            .OrderBy(l => l.Name)
            .ToListAsync();
    }

    public async Task<bool> UserHasAccessToLocationAsync(int userId, int locationId)
    {
        var accessibleLocationIds = await GetUserAccessibleLocationIdsAsync(userId);
        return accessibleLocationIds.Contains(locationId);
    }

    public async Task<List<int>> GetLocationIdsInGroupRecursiveAsync(int locationGroupId)
    {
        var group = await _context.LocationGroups
            .Include(lg => lg.Locations)
            .Include(lg => lg.ChildGroups)
            .FirstOrDefaultAsync(lg => lg.Id == locationGroupId);

        if (group == null) return new List<int>();

        var locationIds = group.Locations.Where(l => l.IsActive).Select(l => l.Id).ToList();

        // Recursively get locations from child groups
        foreach (var childGroup in group.ChildGroups.Where(cg => cg.IsActive))
        {
            var childLocationIds = await GetLocationIdsInGroupRecursiveAsync(childGroup.Id);
            locationIds.AddRange(childLocationIds);
        }

        return locationIds;
    }

    public async Task<List<Location>> GetLocationsInGroupAsync(int locationGroupId, bool recursive = true)
    {
        if (recursive)
        {
            var locationIds = await GetLocationIdsInGroupRecursiveAsync(locationGroupId);
            return await _context.Locations
                .Where(l => locationIds.Contains(l.Id))
                .Include(l => l.LocationGroup)
                .ToListAsync();
        }
        else
        {
            return await _context.Locations
                .Where(l => l.LocationGroupId == locationGroupId && l.IsActive)
                .Include(l => l.LocationGroup)
                .ToListAsync();
        }
    }

    public async Task<LocationGroup> CreateLocationGroupAsync(int organizationId, string name, string? groupType, int? parentGroupId)
    {
        var level = 0;
        if (parentGroupId.HasValue)
        {
            var parent = await _context.LocationGroups.FindAsync(parentGroupId.Value);
            if (parent != null)
            {
                level = parent.Level + 1;
            }
        }

        var locationGroup = new LocationGroup
        {
            OrganizationId = organizationId,
            ParentGroupId = parentGroupId,
            Name = name,
            GroupType = groupType,
            Level = level,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.LocationGroups.Add(locationGroup);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created location group {GroupName} (ID: {GroupId}) for organization {OrgId}",
            name, locationGroup.Id, organizationId);

        return locationGroup;
    }

    public async Task<Location> CreateLocationAsync(int organizationId, string name, string address, string city, string state, string zipCode, int? locationGroupId)
    {
        var location = new Location
        {
            OrganizationId = organizationId,
            LocationGroupId = locationGroupId,
            Name = name,
            Address = address,
            City = city,
            State = state,
            ZipCode = zipCode,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created location {LocationName} (ID: {LocationId}) for organization {OrgId}",
            name, location.Id, organizationId);

        return location;
    }

    public async Task AssignUserToLocationsAsync(int userId, List<int> locationIds)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.OrganizationId == null)
        {
            throw new InvalidOperationException("User not found or not assigned to organization");
        }

        // Remove existing location-specific accesses
        var existing = await _context.UserLocationAccesses
            .Where(ula => ula.UserId == userId && ula.LocationId.HasValue)
            .ToListAsync();

        _context.UserLocationAccesses.RemoveRange(existing);

        // Add new accesses
        foreach (var locationId in locationIds)
        {
            var access = new UserLocationAccess
            {
                UserId = userId,
                OrganizationId = user.OrganizationId.Value,
                LocationId = locationId,
                HasAllLocationsAccess = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserLocationAccesses.Add(access);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned user {UserId} to {Count} locations", userId, locationIds.Count);
    }

    public async Task AssignUserToLocationGroupAsync(int userId, int locationGroupId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.OrganizationId == null)
        {
            throw new InvalidOperationException("User not found or not assigned to organization");
        }

        var access = new UserLocationAccess
        {
            UserId = userId,
            OrganizationId = user.OrganizationId.Value,
            LocationGroupId = locationGroupId,
            HasAllLocationsAccess = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserLocationAccesses.Add(access);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned user {UserId} to location group {GroupId}", userId, locationGroupId);
    }

    public async Task AssignUserToAllLocationsAsync(int userId, int organizationId)
    {
        // Remove all existing accesses for this user in this organization
        var existing = await _context.UserLocationAccesses
            .Where(ula => ula.UserId == userId && ula.OrganizationId == organizationId)
            .ToListAsync();

        _context.UserLocationAccesses.RemoveRange(existing);

        // Add "all locations" access
        var access = new UserLocationAccess
        {
            UserId = userId,
            OrganizationId = organizationId,
            HasAllLocationsAccess = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.UserLocationAccesses.Add(access);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Assigned user {UserId} to ALL locations in organization {OrgId}", userId, organizationId);
    }
}
