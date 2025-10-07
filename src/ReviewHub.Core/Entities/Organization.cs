namespace ReviewHub.Core.Entities;

/// <summary>
/// Top-level entity representing the entire company/enterprise
/// Example: "Joe's Restaurant Chain", "ABC Dental Group"
/// </summary>
public class Organization
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LogoUrl { get; set; }

    // Hierarchy Configuration
    /// <summary>
    /// JSON: ["Region", "State", "City"] or ["Division", "Department", "Office"]
    /// Defines custom hierarchy levels for this organization
    /// </summary>
    public string? HierarchyLevels { get; set; }

    // Subscription & Billing
    public string SubscriptionPlan { get; set; } = "Free";
    public DateTime? SubscriptionExpiresAt { get; set; }
    public int MaxLocations { get; set; } = 1; // Based on plan
    public int MaxUsers { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Location> Locations { get; set; } = new List<Location>();
    public ICollection<LocationGroup> LocationGroups { get; set; } = new List<LocationGroup>();
    public ICollection<Business> Businesses { get; set; } = new List<Business>(); // Legacy compatibility
}
