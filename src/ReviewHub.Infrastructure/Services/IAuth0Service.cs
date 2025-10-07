namespace ReviewHub.Infrastructure.Services;

public interface IAuth0Service
{
    /// <summary>
    /// Create a new user in Auth0 with MFA enforced
    /// </summary>
    Task<string> CreateUserAsync(string email, string password, string fullName, string? phoneNumber = null);

    /// <summary>
    /// Update user metadata in Auth0
    /// </summary>
    Task UpdateUserMetadataAsync(string auth0UserId, Dictionary<string, object> metadata);

    /// <summary>
    /// Get Auth0 user by email
    /// </summary>
    Task<Auth0UserInfo?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Enforce MFA for a user
    /// </summary>
    Task EnforceMFAAsync(string auth0UserId);
}

public class Auth0UserInfo
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}
