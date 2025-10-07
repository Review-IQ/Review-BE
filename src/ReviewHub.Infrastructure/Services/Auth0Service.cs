using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ReviewHub.Infrastructure.Services;

public class Auth0Service : IAuth0Service
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<Auth0Service> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _domain;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public Auth0Service(
        IConfiguration configuration,
        ILogger<Auth0Service> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;

        _domain = _configuration["Auth0:Domain"] ?? throw new ArgumentNullException("Auth0:Domain not configured");
        _clientId = _configuration["Auth0:ClientId"] ?? throw new ArgumentNullException("Auth0:ClientId not configured");
        _clientSecret = _configuration["Auth0:ClientSecret"] ?? throw new ArgumentNullException("Auth0:ClientSecret not configured");
    }

    public async Task<string> CreateUserAsync(string email, string password, string fullName, string? phoneNumber = null)
    {
        try
        {
            var client = await GetManagementApiClientAsync();

            var userCreateRequest = new UserCreateRequest
            {
                Email = email,
                Password = password,
                FullName = fullName,
                Connection = "Username-Password-Authentication",
                EmailVerified = false, // User will verify email
                VerifyEmail = true, // Send verification email
                UserMetadata = new
                {
                    phone_number = phoneNumber,
                    registration_source = "team_invitation",
                    registered_at = DateTime.UtcNow
                }
            };

            var user = await client.Users.CreateAsync(userCreateRequest);

            // Enforce MFA for the user
            await EnforceMFAAsync(user.UserId);

            _logger.LogInformation("Created Auth0 user {UserId} for email {Email}", user.UserId, email);

            return user.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Auth0 user for email {Email}", email);
            throw new InvalidOperationException($"Failed to create Auth0 user: {ex.Message}", ex);
        }
    }

    public async Task UpdateUserMetadataAsync(string auth0UserId, Dictionary<string, object> metadata)
    {
        try
        {
            var client = await GetManagementApiClientAsync();

            var userUpdateRequest = new UserUpdateRequest
            {
                UserMetadata = metadata
            };

            await client.Users.UpdateAsync(auth0UserId, userUpdateRequest);

            _logger.LogInformation("Updated metadata for Auth0 user {UserId}", auth0UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Auth0 user metadata for {UserId}", auth0UserId);
            throw;
        }
    }

    public async Task<Auth0UserInfo?> GetUserByEmailAsync(string email)
    {
        try
        {
            var client = await GetManagementApiClientAsync();

            var users = await client.Users.GetUsersByEmailAsync(email);

            if (users == null || users.Count == 0)
            {
                return null;
            }

            var user = users[0];
            return new Auth0UserInfo
            {
                UserId = user.UserId,
                Email = user.Email,
                Name = user.FullName ?? user.Email,
                EmailVerified = user.EmailVerified ?? false,
                CreatedAt = user.CreatedAt ?? DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Auth0 user by email {Email}", email);
            return null;
        }
    }

    public async Task EnforceMFAAsync(string auth0UserId)
    {
        try
        {
            var client = await GetManagementApiClientAsync();

            // Set authentication_methods to require MFA
            var userUpdateRequest = new UserUpdateRequest
            {
                AppMetadata = new
                {
                    mfa_required = true
                }
            };

            await client.Users.UpdateAsync(auth0UserId, userUpdateRequest);

            _logger.LogInformation("Enforced MFA for Auth0 user {UserId}", auth0UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing MFA for Auth0 user {UserId}", auth0UserId);
            // Don't throw - MFA enforcement is optional, but log the error
        }
    }

    private async Task<ManagementApiClient> GetManagementApiClientAsync()
    {
        // Get Management API access token
        var accessToken = await GetManagementApiTokenAsync();

        return new ManagementApiClient(accessToken, new Uri($"https://{_domain}/api/v2"));
    }

    private async Task<string> GetManagementApiTokenAsync()
    {
        try
        {
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://{_domain}/oauth/token");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "client_credentials" },
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "audience", $"https://{_domain}/api/v2/" }
            });

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content);

            if (tokenResponse?.AccessToken == null)
            {
                throw new InvalidOperationException("Failed to get Auth0 Management API token");
            }

            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Auth0 Management API token");
            throw;
        }
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
    }
}
