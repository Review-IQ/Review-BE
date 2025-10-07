using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Infrastructure.Data;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SubscriptionController> _logger;
    private readonly IConfiguration _configuration;

    public SubscriptionController(
        ApplicationDbContext context,
        ILogger<SubscriptionController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;

        // Initialize Stripe
        StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
    }

    [HttpGet("plans")]
    public IActionResult GetPlans()
    {
        var plans = new[]
        {
            new
            {
                id = "free",
                name = "Free",
                price = 0,
                interval = "month",
                features = new[]
                {
                    "1 Business Location",
                    "Up to 50 reviews/month",
                    "Basic analytics",
                    "Email support"
                },
                stripePriceId = (string?)null
            },
            new
            {
                id = "pro",
                name = "Pro",
                price = 49,
                interval = "month",
                features = new[]
                {
                    "5 Business Locations",
                    "Unlimited reviews",
                    "Advanced analytics",
                    "AI-powered replies",
                    "SMS campaigns (500/month)",
                    "Priority support"
                },
                stripePriceId = _configuration["Stripe:ProPriceId"]
            },
            new
            {
                id = "enterprise",
                name = "Enterprise",
                price = 149,
                interval = "month",
                features = new[]
                {
                    "Unlimited locations",
                    "Unlimited reviews",
                    "White-label options",
                    "Advanced AI features",
                    "Unlimited SMS campaigns",
                    "Dedicated account manager",
                    "Custom integrations"
                },
                stripePriceId = _configuration["Stripe:EnterprisePriceId"]
            }
        };

        return Ok(plans);
    }

    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var priceId = request.PlanId.ToLower() switch
            {
                "pro" => _configuration["Stripe:ProPriceId"],
                "enterprise" => _configuration["Stripe:EnterprisePriceId"],
                _ => null
            };

            if (string.IsNullOrEmpty(priceId))
            {
                return BadRequest(new { message = "Invalid plan selected" });
            }

            var options = new SessionCreateOptions
            {
                CustomerEmail = user.Email,
                ClientReferenceId = user.Id.ToString(),
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = $"{_configuration["App:FrontendUrl"]}/settings?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{_configuration["App:FrontendUrl"]}/settings?canceled=true",
                Metadata = new Dictionary<string, string>
                {
                    { "userId", user.Id.ToString() },
                    { "plan", request.PlanId }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new { sessionId = session.Id, url = session.Url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checkout session");
            return StatusCode(500, new { message = "Failed to create checkout session" });
        }
    }

    [HttpPost("create-portal-session")]
    public async Task<IActionResult> CreatePortalSession()
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                return BadRequest(new { message = "No active subscription found" });
            }

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = user.StripeCustomerId,
                ReturnUrl = $"{_configuration["App:FrontendUrl"]}/settings",
            };

            var service = new Stripe.BillingPortal.SessionService();
            var session = await service.CreateAsync(options);

            return Ok(new { url = session.Url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portal session");
            return StatusCode(500, new { message = "Failed to create portal session" });
        }
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                plan = user.SubscriptionPlan ?? "Free",
                expiresAt = user.SubscriptionExpiresAt,
                stripeCustomerId = user.StripeCustomerId,
                isActive = user.SubscriptionExpiresAt == null || user.SubscriptionExpiresAt > DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription");
            return StatusCode(500, new { message = "Failed to get subscription" });
        }
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription()
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            if (string.IsNullOrEmpty(user.StripeCustomerId))
            {
                return BadRequest(new { message = "No active subscription found" });
            }

            // Get customer's subscriptions
            var subscriptionService = new SubscriptionService();
            var subscriptions = await subscriptionService.ListAsync(new SubscriptionListOptions
            {
                Customer = user.StripeCustomerId,
                Status = "active"
            });

            if (subscriptions.Data.Count == 0)
            {
                return BadRequest(new { message = "No active subscription found" });
            }

            // Cancel at period end
            var subscription = subscriptions.Data[0];
            await subscriptionService.UpdateAsync(subscription.Id, new SubscriptionUpdateOptions
            {
                CancelAtPeriodEnd = true
            });

            _logger.LogInformation("Subscription cancelled for user {UserId}", user.Id);

            return Ok(new { message = "Subscription will be cancelled at period end" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling subscription");
            return StatusCode(500, new { message = "Failed to cancel subscription" });
        }
    }
}

public record CreateCheckoutRequest(string PlanId);
