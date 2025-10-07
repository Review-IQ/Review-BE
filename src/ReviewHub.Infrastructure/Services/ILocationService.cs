using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Services;

public interface ILocationService
{
    Task<List<int>> GetUserAccessibleLocationIdsAsync(int userId);
    Task<List<Location>> GetUserAccessibleLocationsAsync(int userId);
    Task<bool> UserHasAccessToLocationAsync(int userId, int locationId);
    Task<LocationGroup> CreateLocationGroupAsync(int organizationId, string name, string? groupType, int? parentGroupId);
    Task<Location> CreateLocationAsync(int organizationId, string name, string address, string city, string state, string zipCode, int? locationGroupId);
    Task AssignUserToLocationsAsync(int userId, List<int> locationIds);
    Task AssignUserToLocationGroupAsync(int userId, int locationGroupId);
    Task AssignUserToAllLocationsAsync(int userId, int organizationId);
    Task<List<Location>> GetLocationsInGroupAsync(int locationGroupId, bool recursive = true);
    Task<List<int>> GetLocationIdsInGroupRecursiveAsync(int locationGroupId);
}
