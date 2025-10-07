namespace ReviewHub.Core.Entities;

/// <summary>
/// Flexible hierarchy grouping for locations
/// Examples: "West Coast Region", "California", "Los Angeles District"
/// Supports self-referencing for unlimited nesting
/// </summary>
public class LocationGroup
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? ParentGroupId { get; set; } // Self-reference for nesting

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Type from organization's HierarchyLevels
    /// Example: "Region", "State", "City", "Division", etc.
    /// </summary>
    public string? GroupType { get; set; }

    /// <summary>
    /// Order in hierarchy (0 = top level, 1 = second level, etc.)
    /// </summary>
    public int Level { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Organization Organization { get; set; } = null!;
    public LocationGroup? ParentGroup { get; set; }
    public ICollection<LocationGroup> ChildGroups { get; set; } = new List<LocationGroup>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
    public ICollection<UserLocationAccess> UserAccesses { get; set; } = new List<UserLocationAccess>();
}
