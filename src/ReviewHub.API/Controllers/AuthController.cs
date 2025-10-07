using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(
        ApplicationDbContext context,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Initiates Auth0 login flow - redirects to Auth0
    /// </summary>
    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        var domain = _configuration["Auth0:Domain"];
        var clientId = _configuration["Auth0:ClientId"];
        var audience = _configuration["Auth0:Audience"];
        var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";

        var authorizationUrl = $"https://{domain}/authorize?" +
            $"response_type=code&" +
            $"client_id={clientId}&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"scope=openid%20profile%20email&" +
            $"audience={Uri.EscapeDataString(audience!)}&" +
            $"state={Convert.ToBase64String(Encoding.UTF8.GetBytes(returnUrl ?? "/dashboard"))}";

        return Redirect(authorizationUrl);
    }

    /// <summary>
    /// Handles Auth0 callback - exchanges code for token
    /// </summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? state = null)
    {
        try
        {
            var domain = _configuration["Auth0:Domain"];
            var clientId = _configuration["Auth0:ClientId"];
            var clientSecret = _configuration["Auth0:ClientSecret"];
            var audience = _configuration["Auth0:Audience"];
            var redirectUri = $"{Request.Scheme}://{Request.Host}/api/auth/callback";
            var frontendUrl = _configuration["AppUrl"] ?? "http://localhost:5173";

            // Exchange code for tokens
            var httpClient = _httpClientFactory.CreateClient();
            var tokenRequest = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "client_id", clientId! },
                { "client_secret", clientSecret! },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "audience", audience! }
            };

            var response = await httpClient.PostAsync(
                $"https://{domain}/oauth/token",
                new FormUrlEncodedContent(tokenRequest));

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Auth0 token exchange failed: {Error}", error);
                return Redirect($"{frontendUrl}/login?error=auth_failed");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<Auth0TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return Redirect($"{frontendUrl}/login?error=no_token");
            }

            // Decode the return URL from state
            var returnUrl = "/dashboard";
            if (!string.IsNullOrEmpty(state))
            {
                try
                {
                    returnUrl = Encoding.UTF8.GetString(Convert.FromBase64String(state));
                }
                catch { /* Use default */ }
            }

            // Redirect to frontend with tokens
            var redirectUrl = $"{frontendUrl}/auth/callback?" +
                $"access_token={tokenResponse.AccessToken}&" +
                $"id_token={tokenResponse.IdToken}&" +
                $"expires_in={tokenResponse.ExpiresIn}&" +
                $"return_url={Uri.EscapeDataString(returnUrl)}";

            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auth callback");
            var frontendUrl = _configuration["AppUrl"] ?? "http://localhost:5173";
            return Redirect($"{frontendUrl}/login?error=callback_failed");
        }
    }

    /// <summary>
    /// Logout - clears Auth0 session
    /// </summary>
    [HttpGet("logout")]
    public IActionResult Logout([FromQuery] string? returnUrl = null)
    {
        var domain = _configuration["Auth0:Domain"];
        var clientId = _configuration["Auth0:ClientId"];
        var frontendUrl = _configuration["AppUrl"] ?? "http://localhost:5173";

        var logoutUrl = $"https://{domain}/v2/logout?" +
            $"client_id={clientId}&" +
            $"returnTo={Uri.EscapeDataString($"{frontendUrl}/login")}";

        return Redirect(logoutUrl);
    }

    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email) ?? request.Email;

            if (string.IsNullOrEmpty(auth0Id))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (existingUser != null)
            {
                return Ok(new { user = MapToUserDto(existingUser), message = "User already registered" });
            }

            // Create new user
            var user = new User
            {
                Auth0Id = auth0Id,
                Email = email,
                FullName = request.FullName,
                CompanyName = request.CompanyName,
                PhoneNumber = request.PhoneNumber,
                SubscriptionPlan = "Free",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email} (Auth0 ID: {Auth0Id})", email, auth0Id);

            return Ok(new { user = MapToUserDto(user), message = "Registration successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "Registration failed" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(auth0Id))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                // User not found in our system - this means first time login after Auth0 signup
                // Return a flag indicating registration is needed
                return NotFound(new {
                    message = "User not found in system",
                    needsRegistration = true,
                    auth0Id = auth0Id,
                    email = email
                });
            }

            // Check if user account is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted to access system: {Email} (Auth0 ID: {Auth0Id})", user.Email, auth0Id);
                return StatusCode(403, new { message = "Account is inactive. Please contact support." });
            }

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "Failed to get user" });
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.FullName = request.FullName ?? user.FullName;
            user.CompanyName = request.CompanyName ?? user.CompanyName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, new { message = "Failed to update profile" });
        }
    }

    private static object MapToUserDto(User user) => new
    {
        id = user.Id,
        email = user.Email,
        fullName = user.FullName,
        companyName = user.CompanyName,
        phoneNumber = user.PhoneNumber,
        subscriptionPlan = user.SubscriptionPlan,
        subscriptionExpiresAt = user.SubscriptionExpiresAt,
        createdAt = user.CreatedAt
    };
}

public record RegisterRequest(string Email, string FullName, string? CompanyName, string? PhoneNumber);
public record UpdateProfileRequest(string? FullName, string? CompanyName, string? PhoneNumber);

public record Auth0TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("id_token")] string IdToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string TokenType
);
