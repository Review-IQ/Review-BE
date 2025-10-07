namespace ReviewHub.Core.Entities;

/// <summary>
/// Legacy entity - maintained for backward compatibility
/// New multi-location implementations should use Organization → Location instead
/// </summary>
public class Business
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? OrganizationId { get; set; } // NEW: Link to organization (optional for legacy support)

    public string Name { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? Website { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? LogoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
    public Organization? Organization { get; set; }
    public ICollection<PlatformConnection> PlatformConnections { get; set; } = new List<PlatformConnection>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<SmsMessage> SmsMessages { get; set; } = new List<SmsMessage>();
    public ICollection<Competitor> Competitors { get; set; } = new List<Competitor>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
}
