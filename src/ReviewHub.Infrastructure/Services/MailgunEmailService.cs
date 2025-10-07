using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;

namespace ReviewHub.Infrastructure.Services;

public class MailgunEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MailgunEmailService> _logger;
    private readonly string _apiKey;
    private readonly string _domain;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly string _baseUrl;

    public MailgunEmailService(
        IConfiguration configuration,
        ILogger<MailgunEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _apiKey = _configuration["Mailgun:ApiKey"] ?? throw new ArgumentNullException("Mailgun:ApiKey not configured");
        _domain = _configuration["Mailgun:Domain"] ?? throw new ArgumentNullException("Mailgun:Domain not configured");
        _fromEmail = _configuration["Mailgun:FromEmail"] ?? "noreply@reviewhub.com";
        _fromName = _configuration["Mailgun:FromName"] ?? "ReviewHub";
        _baseUrl = $"https://api.mailgun.net/v3/{_domain}";
    }

    public async Task<bool> SendTeamInvitationAsync(string toEmail, string inviterName, string businessName, string invitationToken)
    {
        try
        {
            var acceptUrl = $"{_configuration["AppUrl"]}/accept-invitation?token={invitationToken}";

            var subject = $"{inviterName} invited you to join {businessName} on ReviewHub";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>You're Invited!</h1>
        </div>
        <div class=""content"">
            <p>Hi there,</p>
            <p><strong>{inviterName}</strong> has invited you to join the team at <strong>{businessName}</strong> on ReviewHub.</p>
            <p>ReviewHub helps businesses manage their online reviews across Google, Yelp, and Facebook - all in one place.</p>
            <p>Click the button below to accept the invitation:</p>
            <div style=""text-align: center;"">
                <a href=""{acceptUrl}"" class=""button"">Accept Invitation</a>
            </div>
            <p style=""color: #666; font-size: 14px;"">This invitation will expire in 7 days.</p>
            <p style=""color: #666; font-size: 14px;"">If the button doesn't work, copy and paste this link into your browser:<br>
            {acceptUrl}</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 ReviewHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            var textBody = $@"
Hi there,

{inviterName} has invited you to join the team at {businessName} on ReviewHub.

ReviewHub helps businesses manage their online reviews across Google, Yelp, and Facebook - all in one place.

Click the link below to accept the invitation:
{acceptUrl}

This invitation will expire in 7 days.

© 2025 ReviewHub. All rights reserved.
";

            return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending team invitation email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
    {
        try
        {
            var subject = "Welcome to ReviewHub!";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .feature {{ margin: 15px 0; padding: 15px; background: white; border-radius: 5px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to ReviewHub!</h1>
        </div>
        <div class=""content"">
            <p>Hi {userName},</p>
            <p>Welcome to ReviewHub! We're excited to have you on board.</p>
            <p>With ReviewHub, you can:</p>
            <div class=""feature"">✨ Manage reviews from Google, Yelp, and Facebook in one place</div>
            <div class=""feature"">🤖 Use AI to generate smart responses to customer reviews</div>
            <div class=""feature"">📊 Get powerful analytics and insights about your business</div>
            <div class=""feature"">👥 Collaborate with your team members</div>
            <div style=""text-align: center;"">
                <a href=""{_configuration["AppUrl"]}/dashboard"" class=""button"">Get Started</a>
            </div>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 ReviewHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            var textBody = $@"
Hi {userName},

Welcome to ReviewHub! We're excited to have you on board.

With ReviewHub, you can:
• Manage reviews from Google, Yelp, and Facebook in one place
• Use AI to generate smart responses to customer reviews
• Get powerful analytics and insights about your business
• Collaborate with your team members

Get started now: {_configuration["AppUrl"]}/dashboard

© 2025 ReviewHub. All rights reserved.
";

            return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNewReviewNotificationAsync(string toEmail, string businessName, string reviewerName, int rating, string reviewText)
    {
        try
        {
            var stars = new string('⭐', rating);
            var subject = $"New {rating}-star review for {businessName}";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .review {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #667eea; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>New Review Received!</h1>
        </div>
        <div class=""content"">
            <p>You have a new review for <strong>{businessName}</strong>!</p>
            <div class=""review"">
                <div style=""font-size: 24px; margin-bottom: 10px;"">{stars}</div>
                <p><strong>{reviewerName}</strong></p>
                <p style=""color: #555;"">{reviewText}</p>
            </div>
            <div style=""text-align: center;"">
                <a href=""{_configuration["AppUrl"]}/reviews"" class=""button"">View & Respond</a>
            </div>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 ReviewHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            var textBody = $@"
You have a new review for {businessName}!

{stars} - {reviewerName}
{reviewText}

View and respond: {_configuration["AppUrl"]}/reviews

© 2025 ReviewHub. All rights reserved.
";

            return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending review notification email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendNotificationEmailAsync(string toEmail, string subject, string body)
    {
        try
        {
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>ReviewHub</h1>
        </div>
        <div class=""content"">
            {body}
        </div>
        <div class=""footer"">
            <p>&copy; 2025 ReviewHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(toEmail, subject, htmlBody, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification email to {Email}", toEmail);
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        try
        {
            var resetUrl = $"{_configuration["AppUrl"]}/reset-password?token={resetToken}";
            var subject = "Reset Your Password - ReviewHub";
            var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .warning {{ background: #fef3c7; padding: 15px; border-radius: 5px; border-left: 4px solid #f59e0b; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Password Reset Request</h1>
        </div>
        <div class=""content"">
            <p>We received a request to reset your password.</p>
            <p>Click the button below to reset your password:</p>
            <div style=""text-align: center;"">
                <a href=""{resetUrl}"" class=""button"">Reset Password</a>
            </div>
            <div class=""warning"">
                <strong>⚠️ Security Notice:</strong> This link will expire in 1 hour. If you didn't request this password reset, please ignore this email.
            </div>
            <p style=""color: #666; font-size: 14px;"">If the button doesn't work, copy and paste this link into your browser:<br>
            {resetUrl}</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 ReviewHub. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            var textBody = $@"
Password Reset Request

We received a request to reset your password.

Click the link below to reset your password:
{resetUrl}

⚠️ Security Notice: This link will expire in 1 hour. If you didn't request this password reset, please ignore this email.

© 2025 ReviewHub. All rights reserved.
";

            return await SendEmailAsync(toEmail, subject, htmlBody, textBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
            return false;
        }
    }

    private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string textBody)
    {
        try
        {
            var options = new RestClientOptions(_baseUrl)
            {
                Authenticator = new HttpBasicAuthenticator("api", _apiKey)
            };

            var client = new RestClient(options);
            var request = new RestRequest("messages", Method.Post);

            request.AddParameter("from", $"{_fromName} <{_fromEmail}>");
            request.AddParameter("to", toEmail);
            request.AddParameter("subject", subject);
            request.AddParameter("html", htmlBody);
            request.AddParameter("text", textBody);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send email to {Email}. Status: {Status}, Error: {Error}",
                    toEmail, response.StatusCode, response.Content);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending email to {Email}", toEmail);
            return false;
        }
    }
}
