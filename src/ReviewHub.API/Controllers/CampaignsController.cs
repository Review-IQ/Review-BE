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
public class CampaignsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly ILogger<CampaignsController> _logger;

    public CampaignsController(
        ApplicationDbContext context,
        ISmsService smsService,
        ILogger<CampaignsController> logger)
    {
        _context = context;
        _smsService = smsService;
        _logger = logger;
    }

    [HttpGet("{businessId}")]
    public async Task<IActionResult> GetCampaigns(int businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
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

            var campaigns = await _context.Campaigns
                .Where(c => c.BusinessId == businessId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Message,
                    c.ScheduledFor,
                    c.Status,
                    c.SentCount,
                    c.TotalRecipients,
                    c.CreatedAt
                })
                .ToListAsync();

            var totalCount = await _context.Campaigns.CountAsync(c => c.BusinessId == businessId);

            return Ok(new
            {
                campaigns = campaigns,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaigns");
            return StatusCode(500, new { message = "Failed to get campaigns" });
        }
    }

    [HttpGet("detail/{id}")]
    public async Task<IActionResult> GetCampaign(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            return Ok(new
            {
                campaign.Id,
                campaign.Name,
                campaign.Message,
                campaign.ScheduledFor,
                campaign.Status,
                campaign.SentCount,
                campaign.TotalRecipients,
                campaign.CreatedAt,
                BusinessName = campaign.Business.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting campaign");
            return StatusCode(500, new { message = "Failed to get campaign" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
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

            if (sentThisMonth + request.RecipientPhoneNumbers.Count > monthlyLimit)
            {
                return BadRequest(new
                {
                    message = $"SMS limit exceeded. Your {plan} plan allows {monthlyLimit} SMS per month. You've already sent {sentThisMonth}."
                });
            }

            var campaign = new Campaign
            {
                BusinessId = request.BusinessId,
                Name = request.Name,
                Message = request.Message,
                ScheduledFor = request.ScheduledFor ?? DateTime.UtcNow,
                Status = request.ScheduledFor.HasValue && request.ScheduledFor > DateTime.UtcNow ? "Scheduled" : "Draft",
                SentCount = 0,
                TotalRecipients = request.RecipientPhoneNumbers.Count,
                CreatedAt = DateTime.UtcNow
            };

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            // If scheduled for now or past, send immediately
            if (campaign.ScheduledFor <= DateTime.UtcNow)
            {
                await ExecuteCampaign(campaign.Id, request.RecipientPhoneNumbers);
            }

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, new
            {
                campaign.Id,
                campaign.Name,
                campaign.Message,
                campaign.ScheduledFor,
                campaign.Status,
                campaign.TotalRecipients,
                campaign.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating campaign");
            return StatusCode(500, new { message = "Failed to create campaign" });
        }
    }

    [HttpPost("{id}/send")]
    public async Task<IActionResult> SendCampaign(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            if (campaign.Status == "Sent")
            {
                return BadRequest(new { message = "Campaign has already been sent" });
            }

            // Get all customers for this business
            var customers = await _context.Customers
                .Where(c => c.BusinessId == campaign.BusinessId)
                .Select(c => c.PhoneNumber)
                .ToListAsync();

            await ExecuteCampaign(id, customers);

            return Ok(new
            {
                campaign.Id,
                campaign.Name,
                Status = "Sent",
                campaign.SentCount,
                campaign.TotalRecipients
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending campaign");
            return StatusCode(500, new { message = "Failed to send campaign" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCampaign(int id, [FromBody] UpdateCampaignRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            if (campaign.Status == "Sent")
            {
                return BadRequest(new { message = "Cannot update a campaign that has already been sent" });
            }

            campaign.Name = request.Name;
            campaign.Message = request.Message;
            campaign.ScheduledFor = request.ScheduledFor ?? campaign.ScheduledFor;
            campaign.Status = request.ScheduledFor.HasValue && request.ScheduledFor > DateTime.UtcNow ? "Scheduled" : "Draft";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                campaign.Id,
                campaign.Name,
                campaign.Message,
                campaign.ScheduledFor,
                campaign.Status
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating campaign");
            return StatusCode(500, new { message = "Failed to update campaign" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCampaign(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var campaign = await _context.Campaigns
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (campaign == null)
            {
                return NotFound(new { message = "Campaign not found" });
            }

            _context.Campaigns.Remove(campaign);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting campaign");
            return StatusCode(500, new { message = "Failed to delete campaign" });
        }
    }

    private async Task ExecuteCampaign(int campaignId, List<string> phoneNumbers)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null) return;

        try
        {
            campaign.Status = "Sending";
            await _context.SaveChangesAsync();

            var messageSids = await _smsService.SendBulkSmsAsync(phoneNumbers, campaign.Message);

            // Log all messages in database
            var smsMessages = messageSids.Select((sid, index) => new SmsMessage
            {
                BusinessId = campaign.BusinessId,
                ToPhoneNumber = phoneNumbers[index],
                MessageBody = campaign.Message,
                Status = "Sent",
                TwilioMessageSid = sid,
                SentAt = DateTime.UtcNow
            }).ToList();

            _context.SmsMessages.AddRange(smsMessages);

            campaign.Status = "Sent";
            campaign.SentCount = messageSids.Count;
            campaign.TotalRecipients = phoneNumbers.Count;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Campaign {CampaignId} sent successfully to {Count} recipients", campaignId, messageSids.Count.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing campaign {CampaignId}", campaignId);
            campaign.Status = "Failed";
            await _context.SaveChangesAsync();
        }
    }
}

public record CreateCampaignRequest(
    int BusinessId,
    string Name,
    string Message,
    DateTime? ScheduledFor,
    List<string> RecipientPhoneNumbers);

public record UpdateCampaignRequest(
    string Name,
    string Message,
    DateTime? ScheduledFor);
