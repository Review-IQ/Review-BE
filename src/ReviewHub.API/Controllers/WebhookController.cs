using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using ReviewHub.Infrastructure.Services;
using Stripe;
using Stripe.Checkout;
using System.Text.Json;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebhookController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IGoogleBusinessService _googleBusinessService;
    private readonly IFacebookService _facebookService;
    private readonly INotificationService _notificationService;

    public WebhookController(
        ApplicationDbContext context,
        ILogger<WebhookController> logger,
        IConfiguration configuration,
        IGoogleBusinessService googleBusinessService,
        IFacebookService facebookService,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _googleBusinessService = googleBusinessService;
        _facebookService = facebookService;
        _notificationService = notificationService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"];

        try
        {
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                webhookSecret
            );

            _logger.LogInformation("Stripe webhook received: {EventType}", stripeEvent.Type);

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompleted(stripeEvent);
                    break;

                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdated(stripeEvent);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeleted(stripeEvent);
                    break;

                case "invoice.payment_succeeded":
                    await HandleInvoicePaymentSucceeded(stripeEvent);
                    break;

                case "invoice.payment_failed":
                    await HandleInvoicePaymentFailed(stripeEvent);
                    break;

                default:
                    _logger.LogInformation("Unhandled webhook event: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }

    private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session == null) return;

        var userId = int.Parse(session.ClientReferenceId);
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            _logger.LogWarning("User not found for checkout session: {UserId}", userId);
            return;
        }

        user.StripeCustomerId = session.CustomerId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Checkout completed for user {UserId}, customer {CustomerId}",
            userId, session.CustomerId);
    }

    private async Task HandleSubscriptionUpdated(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);

        if (user == null)
        {
            _logger.LogWarning("User not found for subscription update: {CustomerId}", subscription.CustomerId);
            return;
        }

        // Determine plan from price ID
        var priceId = subscription.Items.Data[0].Price.Id;
        var plan = DeterminePlanFromPriceId(priceId);

        user.SubscriptionPlan = plan;
        // Set subscription expiration (Stripe subscription will auto-renew)
        // We'll update this when we receive invoice.payment_succeeded events
        user.SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(1); // Default to 1 month from now
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription updated for user {UserId}: {Plan}",
            user.Id, plan);
    }

    private async Task HandleSubscriptionDeleted(Event stripeEvent)
    {
        var subscription = stripeEvent.Data.Object as Subscription;
        if (subscription == null) return;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.StripeCustomerId == subscription.CustomerId);

        if (user == null)
        {
            _logger.LogWarning("User not found for subscription deletion: {CustomerId}", subscription.CustomerId);
            return;
        }

        user.SubscriptionPlan = "Free";
        user.SubscriptionExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Subscription deleted for user {UserId}, reverted to Free plan", user.Id);
    }

    private async Task HandleInvoicePaymentSucceeded(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        _logger.LogInformation("Payment succeeded for customer {CustomerId}, amount {Amount}",
            invoice.CustomerId, invoice.AmountPaid / 100.0);

        // Optional: Send payment confirmation email via Mailgun
        // await _emailService.SendPaymentConfirmation(invoice);
    }

    private async Task HandleInvoicePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice == null) return;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.StripeCustomerId == invoice.CustomerId);

        if (user == null)
        {
            _logger.LogWarning("User not found for failed payment: {CustomerId}", invoice.CustomerId);
            return;
        }

        _logger.LogWarning("Payment failed for user {UserId}, amount {Amount}",
            user.Id, invoice.AmountDue / 100.0);

        // Optional: Send payment failure notification email
        // await _emailService.SendPaymentFailureNotification(user, invoice);
    }

    private string DeterminePlanFromPriceId(string priceId)
    {
        var proPriceId = _configuration["Stripe:ProPriceId"];
        var enterprisePriceId = _configuration["Stripe:EnterprisePriceId"];

        if (priceId == proPriceId) return "Pro";
        if (priceId == enterprisePriceId) return "Enterprise";

        return "Free";
    }

    /// <summary>
    /// Google review notification webhook
    /// </summary>
    [HttpPost("google/review")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleReviewWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInformation("Received Google review webhook");

            var notification = JsonSerializer.Deserialize<GoogleWebhookNotification>(json);

            if (notification == null)
            {
                _logger.LogWarning("Failed to deserialize Google webhook notification");
                return BadRequest();
            }

            // Find the connection by location name
            var connection = await _context.PlatformConnections
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c =>
                    c.Platform == ReviewPlatform.Google &&
                    c.PlatformBusinessId == notification.LocationName);

            if (connection == null)
            {
                _logger.LogWarning("No connection found for Google location: {LocationName}", notification.LocationName);
                return Ok(); // Still return 200 to acknowledge receipt
            }

            // Fetch the latest reviews
            var newReviews = await _googleBusinessService.FetchReviewsAsync(connection.Id);

            // Send notifications for each new review
            foreach (var review in newReviews)
            {
                await _notificationService.SendReviewNotificationAsync(
                    connection.BusinessId,
                    "Google",
                    review);
            }

            _logger.LogInformation("Processed Google webhook, found {Count} new reviews", newReviews.Count);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google review webhook");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Facebook webhook verification (required by Facebook)
    /// </summary>
    [HttpGet("facebook")]
    [AllowAnonymous]
    public IActionResult FacebookWebhookVerify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromQuery(Name = "hub.verify_token")] string verifyToken)
    {
        var expectedToken = _configuration["Facebook:WebhookVerifyToken"] ?? "reviewhub_verify_token_2025";

        if (mode == "subscribe" && verifyToken == expectedToken)
        {
            _logger.LogInformation("Facebook webhook verified successfully");
            return Content(challenge); // Return challenge as-is
        }

        _logger.LogWarning("Facebook webhook verification failed");
        return Forbid();
    }

    /// <summary>
    /// Facebook review notification webhook
    /// </summary>
    [HttpPost("facebook")]
    [AllowAnonymous]
    public async Task<IActionResult> FacebookWebhook()
    {
        try
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            _logger.LogInformation("Received Facebook webhook");

            var payload = JsonSerializer.Deserialize<FacebookWebhookPayload>(json);

            if (payload == null || payload.Entry == null)
            {
                _logger.LogWarning("Failed to deserialize Facebook webhook payload");
                return BadRequest();
            }

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;

                foreach (var change in entry.Changes)
                {
                    if (change.Field == "ratings" && change.Value != null)
                    {
                        // Find the connection by page ID
                        var connection = await _context.PlatformConnections
                            .Include(c => c.Business)
                            .FirstOrDefaultAsync(c =>
                                c.Platform == ReviewPlatform.Facebook &&
                                c.PlatformBusinessId == entry.Id);

                        if (connection == null)
                        {
                            _logger.LogWarning("No connection found for Facebook page: {PageId}", entry.Id);
                            continue;
                        }

                        // Fetch the latest reviews
                        var newReviews = await _facebookService.FetchReviewsAsync(connection.Id);

                        // Send notifications for each new review
                        foreach (var review in newReviews)
                        {
                            await _notificationService.SendReviewNotificationAsync(
                                connection.BusinessId,
                                "Facebook",
                                review);
                        }

                        _logger.LogInformation("Processed Facebook webhook, found {Count} new reviews for page {PageId}",
                            newReviews.Count, entry.Id);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Facebook webhook");
            return StatusCode(500);
        }
    }
}

// Google Webhook DTOs
public class GoogleWebhookNotification
{
    public string NotificationType { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ReviewName { get; set; } = string.Empty;
}

// Facebook Webhook DTOs
public class FacebookWebhookPayload
{
    public string Object { get; set; } = string.Empty;
    public FacebookEntry[]? Entry { get; set; }
}

public class FacebookEntry
{
    public string Id { get; set; } = string.Empty;
    public long Time { get; set; }
    public FacebookChange[]? Changes { get; set; }
}

public class FacebookChange
{
    public string Field { get; set; } = string.Empty;
    public FacebookRatingValue? Value { get; set; }
}

public class FacebookRatingValue
{
    public string ReviewerId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? ReviewText { get; set; }
}
