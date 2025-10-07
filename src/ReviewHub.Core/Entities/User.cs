namespace ReviewHub.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Auth0Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? PhoneNumber { get; set; }

    // Multi-location support
    public int? OrganizationId { get; set; } // Which organization this user belongs to
    public string? Role { get; set; } // Admin, Manager, Viewer, etc.

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Stripe
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? SubscriptionPlan { get; set; } // Free, Pro, Enterprise
    public DateTime? SubscriptionExpiresAt { get; set; }

    // Navigation
    public Organization? Organization { get; set; }
    public ICollection<Business> Businesses { get; set; } = new List<Business>();
    public ICollection<UserLocationAccess> LocationAccesses { get; set; } = new List<UserLocationAccess>();
}
