using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Services;

public interface INotificationService
{
    Task SendReviewNotificationAsync(int businessId, string platform, Review review);
    Task CreateInAppNotificationAsync(int userId, string title, string message, object? data = null);
    Task SendEmailNotificationAsync(string toEmail, string subject, string htmlBody);
}
