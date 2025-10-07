using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace ReviewHub.Infrastructure.Services;

public interface ISmsService
{
    Task<string> SendSmsAsync(string to, string message);
    Task<List<string>> SendBulkSmsAsync(List<string> phoneNumbers, string message);
}

public class TwilioSmsService : ISmsService
{
    private readonly ILogger<TwilioSmsService> _logger;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public TwilioSmsService(IConfiguration configuration, ILogger<TwilioSmsService> logger)
    {
        _logger = logger;
        _accountSid = configuration["Twilio:AccountSid"] ?? throw new InvalidOperationException("Twilio AccountSid not configured");
        _authToken = configuration["Twilio:AuthToken"] ?? throw new InvalidOperationException("Twilio AuthToken not configured");
        _fromNumber = configuration["Twilio:PhoneNumber"] ?? throw new InvalidOperationException("Twilio PhoneNumber not configured");

        TwilioClient.Init(_accountSid, _authToken);
    }

    public async Task<string> SendSmsAsync(string to, string message)
    {
        try
        {
            var messageResource = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_fromNumber),
                to: new PhoneNumber(to)
            );

            _logger.LogInformation("SMS sent to {PhoneNumber}, SID: {MessageSid}", to, messageResource.Sid);

            return messageResource.Sid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", to);
            throw;
        }
    }

    public async Task<List<string>> SendBulkSmsAsync(List<string> phoneNumbers, string message)
    {
        var messageSids = new List<string>();
        var errors = new List<string>();

        foreach (var phoneNumber in phoneNumbers)
        {
            try
            {
                var sid = await SendSmsAsync(phoneNumber, message);
                messageSids.Add(sid);

                // Small delay to avoid rate limiting
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SMS to {PhoneNumber} in bulk send", phoneNumber);
                errors.Add(phoneNumber);
            }
        }

        if (errors.Any())
        {
            _logger.LogWarning("Bulk SMS send completed with {SuccessCount}/{TotalCount} successful. Failed numbers: {FailedNumbers}",
                messageSids.Count, phoneNumbers.Count, string.Join(", ", errors));
        }
        else
        {
            _logger.LogInformation("Bulk SMS send completed successfully. Sent {Count} messages", messageSids.Count);
        }

        return messageSids;
    }
}
