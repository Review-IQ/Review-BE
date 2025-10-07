using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;
using System.Text.Json;

namespace ReviewHub.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendReviewNotificationAsync(int businessId, string platform, Review review)
    {
        try
        {
            // Get business and user info
            var business = await _context.Businesses
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null || business.User == null)
            {
                _logger.LogWarning("Business or user not found for notification. BusinessId: {BusinessId}", businessId);
                return;
            }

            // Check user notification preferences
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(p => p.UserId == business.UserId);

            // Create default preferences if not exists
            if (preferences == null)
            {
                preferences = new NotificationPreference
                {
                    UserId = business.UserId,
                    EmailNotifications = true,
                    NotifyOnNewReview = true,
                    NotifyOnLowRating = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.NotificationPreferences.Add(preferences);
                await _context.SaveChangesAsync();
            }

            // Check if user wants to be notified
            if (!preferences.NotifyOnNewReview)
            {
                _logger.LogInformation("User {UserId} has disabled new review notifications", business.UserId);
                return;
            }

            // Check if it's a low rating and user wants alerts
            var isLowRating = review.Rating <= 2;
            if (isLowRating && !preferences.NotifyOnLowRating)
            {
                _logger.LogInformation("User {UserId} has disabled low rating alerts", business.UserId);
                return;
            }

            // Create in-app notification
            var notificationType = isLowRating ? NotificationType.LowRatingAlert : NotificationType.NewReview;
            var title = isLowRating
                ? $"⚠️ Low Rating Alert - {platform}"
                : $"New {platform} Review";

            var message = $"{review.ReviewerName} left a {review.Rating}-star review";

            await CreateInAppNotificationAsync(
                business.UserId,
                title,
                message,
                new { ReviewId = review.Id, BusinessId = businessId, Platform = platform }
            );

            // Send email if enabled
            if (preferences.EmailNotifications)
            {
                await SendReviewEmailAsync(business.User.Email, platform, review, business.Name);
            }

            _logger.LogInformation("Sent notifications for new {Platform} review to user {UserId}",
                platform, business.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending review notification for business {BusinessId}", businessId);
        }
    }

    public async Task CreateInAppNotificationAsync(int userId, string title, string message, object? data = null)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = NotificationType.NewReview,
                Title = title,
                Message = message,
                Data = data != null ? JsonSerializer.Serialize(data) : null,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created in-app notification for user {UserId}: {Title}", userId, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating in-app notification for user {UserId}", userId);
        }
    }

    public async Task SendEmailNotificationAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            // TODO: Implement with Mailgun or SendGrid
            // For now, just log it
            _logger.LogInformation("Email would be sent to {Email} with subject: {Subject}", toEmail, subject);

            // Placeholder for actual email sending
            // var apiKey = _configuration["Mailgun:ApiKey"];
            // var domain = _configuration["Mailgun:Domain"];
            // ... send email via Mailgun API

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", toEmail);
        }
    }

    private async Task SendReviewEmailAsync(string toEmail, string platform, Review review, string businessName)
    {
        var ratingStars = new string('⭐', review.Rating);
        var sentimentEmoji = review.Sentiment switch
        {
            "Positive" => "😊",
            "Negative" => "😟",
            _ => "😐"
        };

        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var reviewUrl = $"{frontendUrl}/reviews?id={review.Id}";

        var subject = review.Rating <= 2
            ? $"⚠️ Low Rating Alert - {platform} Review for {businessName}"
            : $"New {platform} Review for {businessName}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4F46E5; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 20px; border: 1px solid #e5e7eb; }}
        .review-box {{ background-color: white; padding: 20px; border-radius: 8px; margin: 20px 0; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }}
        .rating {{ font-size: 24px; margin: 10px 0; }}
        .sentiment {{ display: inline-block; padding: 5px 15px; border-radius: 20px; font-size: 14px; }}
        .sentiment.positive {{ background-color: #D1FAE5; color: #065F46; }}
        .sentiment.negative {{ background-color: #FEE2E2; color: #991B1B; }}
        .sentiment.neutral {{ background-color: #E5E7EB; color: #374151; }}
        .button {{ display: inline-block; background-color: #4F46E5; color: white; padding: 12px 30px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #6B7280; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>{sentimentEmoji} New Review Notification</h1>
        </div>
        <div class=""content"">
            <div class=""review-box"">
                <p style=""color: #6B7280; font-size: 14px; margin: 0;"">{platform} Review</p>
                <h2 style=""margin: 10px 0;"">{businessName}</h2>

                <div class=""rating"">{ratingStars} ({review.Rating}/5)</div>

                <p><strong>From:</strong> {review.ReviewerName}</p>
                <p><strong>Date:</strong> {review.ReviewDate:MMMM dd, yyyy}</p>

                <div class=""sentiment {review.Sentiment.ToLower()}"">
                    {review.Sentiment}
                </div>

                {(string.IsNullOrEmpty(review.ReviewText) ? "" : $@"
                <div style=""margin: 20px 0; padding: 15px; background-color: #F3F4F6; border-left: 4px solid #4F46E5; border-radius: 4px;"">
                    <p style=""margin: 0; font-style: italic;"">""{review.ReviewText}""</p>
                </div>
                ")}

                <a href=""{reviewUrl}"" class=""button"">View & Reply to Review</a>
            </div>

            {(review.Rating <= 2 ? @"
            <div style=""background-color: #FEF3C7; border: 1px solid #F59E0B; border-radius: 8px; padding: 15px; margin-top: 20px;"">
                <p style=""margin: 0; color: #92400E;"">
                    <strong>⚠️ Low Rating Alert:</strong> This review requires immediate attention.
                    Consider responding promptly to address the customer's concerns.
                </p>
            </div>
            " : "")}
        </div>
        <div class=""footer"">
            <p>You're receiving this email because you have notifications enabled in ReviewHub.</p>
            <p><a href=""{frontendUrl}/settings"" style=""color: #4F46E5;"">Manage notification preferences</a></p>
            <p style=""margin-top: 20px;"">&copy; 2025 ReviewHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailNotificationAsync(toEmail, subject, htmlBody);
    }
}
