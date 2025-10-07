namespace ReviewHub.Core.Entities;

/// <summary>
/// Individual physical business location
/// Example: "Joe's Restaurant - Downtown LA", "ABC Dental - Portland Office"
/// </summary>
public class Location
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int? LocationGroupId { get; set; } // Optional parent group

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Address Information
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Contact Information
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Business Hours (JSON: {monday: "9am-5pm", ...})
    public string? BusinessHours { get; set; }

    // Location-specific branding
    public string? LogoUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    // Manager/Contact for this location
    public int? ManagerUserId { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation Properties
    public Organization Organization { get; set; } = null!;
    public LocationGroup? LocationGroup { get; set; }
    public User? Manager { get; set; }

    public ICollection<PlatformConnection> PlatformConnections { get; set; } = new List<PlatformConnection>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<SmsMessage> SmsMessages { get; set; } = new List<SmsMessage>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    public ICollection<Competitor> Competitors { get; set; } = new List<Competitor>();
    public ICollection<UserLocationAccess> UserAccesses { get; set; } = new List<UserLocationAccess>();
}
