using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using ReviewHub.Infrastructure.Services;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SmsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly ILogger<SmsController> _logger;

    public SmsController(
        ApplicationDbContext context,
        ISmsService smsService,
        ILogger<SmsController> logger)
    {
        _context = context;
        _smsService = smsService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendSms([FromBody] SendSmsRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            // Check subscription limits
            // TODO: Implement subscription-based SMS limit checks

            var messageSid = await _smsService.SendSmsAsync(request.PhoneNumber, request.Message);

            // Log the SMS in database
            var smsMessage = new SmsMessage
            {
                BusinessId = request.BusinessId,
                ToPhoneNumber = request.PhoneNumber,
                MessageBody = request.Message,
                Status = "Sent",
                TwilioMessageSid = messageSid,
                SentAt = DateTime.UtcNow
            };

            _context.SmsMessages.Add(smsMessage);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                messageSid = messageSid,
                message = "SMS sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS");
            return StatusCode(500, new { message = "Failed to send SMS" });
        }
    }

    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulkSms([FromBody] SendBulkSmsRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            // Check subscription limits
            var plan = user.SubscriptionPlan ?? "Free";
            var monthlyLimit = plan switch
            {
                "Pro" => 500,
                "Enterprise" => int.MaxValue,
                _ => 10 // Free plan
            };

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var sentThisMonth = await _context.SmsMessages
                .Where(s => s.BusinessId == request.BusinessId &&
                            s.SentAt.Month == currentMonth &&
                            s.SentAt.Year == currentYear)
                .CountAsync();

            if (sentThisMonth + request.PhoneNumbers.Count > monthlyLimit)
            {
                return BadRequest(new
                {
                    message = $"SMS limit exceeded. Your {plan} plan allows {monthlyLimit} SMS per month. You've already sent {sentThisMonth}."
                });
            }

            var messageSids = await _smsService.SendBulkSmsAsync(request.PhoneNumbers, request.Message);

            // Log all messages in database
            var smsMessages = messageSids.Select((sid, index) => new SmsMessage
            {
                BusinessId = request.BusinessId,
                ToPhoneNumber = request.PhoneNumbers[index],
                MessageBody = request.Message,
                Status = "Sent",
                TwilioMessageSid = sid,
                SentAt = DateTime.UtcNow
            }).ToList();

            _context.SmsMessages.AddRange(smsMessages);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                sentCount = messageSids.Count,
                totalRequested = request.PhoneNumbers.Count,
                message = $"Successfully sent {messageSids.Count} out of {request.PhoneNumbers.Count} SMS messages"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending bulk SMS");
            return StatusCode(500, new { message = "Failed to send bulk SMS" });
        }
    }

    [HttpGet("messages/{businessId}")]
    public async Task<IActionResult> GetMessages(int businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var messages = await _context.SmsMessages
                .Where(s => s.BusinessId == businessId)
                .OrderByDescending(s => s.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    PhoneNumber = s.ToPhoneNumber,
                    Message = s.MessageBody,
                    s.Status,
                    s.SentAt,
                    TwilioSid = s.TwilioMessageSid
                })
                .ToListAsync();

            var totalCount = await _context.SmsMessages.CountAsync(s => s.BusinessId == businessId);

            return Ok(new
            {
                messages = messages,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS messages");
            return StatusCode(500, new { message = "Failed to get messages" });
        }
    }

    [HttpGet("usage/{businessId}")]
    public async Task<IActionResult> GetUsage(int businessId)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            var sentThisMonth = await _context.SmsMessages
                .Where(s => s.BusinessId == businessId &&
                            s.SentAt.Month == currentMonth &&
                            s.SentAt.Year == currentYear)
                .CountAsync();

            var plan = user.SubscriptionPlan ?? "Free";
            var monthlyLimit = plan switch
            {
                "Pro" => 500,
                "Enterprise" => int.MaxValue,
                _ => 10
            };

            return Ok(new
            {
                plan = plan,
                sentThisMonth = sentThisMonth,
                monthlyLimit = monthlyLimit,
                remaining = monthlyLimit - sentThisMonth,
                percentageUsed = monthlyLimit > 0 ? (double)sentThisMonth / monthlyLimit * 100 : 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SMS usage");
            return StatusCode(500, new { message = "Failed to get usage" });
        }
    }
}

public record SendSmsRequest(int BusinessId, string PhoneNumber, string Message);
public record SendBulkSmsRequest(int BusinessId, List<string> PhoneNumbers, string Message);
