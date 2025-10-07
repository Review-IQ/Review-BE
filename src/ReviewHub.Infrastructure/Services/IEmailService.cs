namespace ReviewHub.Infrastructure.Services;

public interface IEmailService
{
    /// <summary>
    /// Send a team invitation email
    /// </summary>
    Task<bool> SendTeamInvitationAsync(string toEmail, string inviterName, string businessName, string invitationToken);

    /// <summary>
    /// Send a welcome email to a new user
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);

    /// <summary>
    /// Send a notification email for a new review
    /// </summary>
    Task<bool> SendNewReviewNotificationAsync(string toEmail, string businessName, string reviewerName, int rating, string reviewText);

    /// <summary>
    /// Send a generic notification email
    /// </summary>
    Task<bool> SendNotificationEmailAsync(string toEmail, string subject, string body);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken);
}
