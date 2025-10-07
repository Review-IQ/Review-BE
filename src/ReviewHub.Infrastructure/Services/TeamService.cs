using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using System.Security.Cryptography;

namespace ReviewHub.Infrastructure.Services;

public class TeamService : ITeamService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<TeamService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public TeamService(
        ApplicationDbContext context,
        ILogger<TeamService> logger,
        INotificationService notificationService,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<TeamInvitation> InviteUserAsync(int businessId, int invitedByUserId, string email, string role)
    {
        // Validate permissions
        if (!await CanManageTeamAsync(businessId, invitedByUserId))
        {
            throw new UnauthorizedAccessException("You don't have permission to invite users");
        }

        // Check if user is already a team member
        var existingMember = await _context.BusinessUsers
            .AnyAsync(bu => bu.BusinessId == businessId && bu.User.Email == email);

        if (existingMember)
        {
            throw new InvalidOperationException("User is already a team member");
        }

        // Check for existing pending invitation
        var existingInvitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(ti => ti.BusinessId == businessId &&
                                      ti.Email == email &&
                                      ti.Status == InvitationStatus.Pending);

        if (existingInvitation != null)
        {
            throw new InvalidOperationException("An invitation has already been sent to this email");
        }

        // Create invitation
        var invitation = new TeamInvitation
        {
            BusinessId = businessId,
            InvitedByUserId = invitedByUserId,
            Email = email,
            Role = role,
            Token = GenerateSecureToken(),
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 7 days expiration
        };

        _context.TeamInvitations.Add(invitation);
        await _context.SaveChangesAsync();

        // Send invitation email
        var inviter = await _context.Users.FindAsync(invitedByUserId);
        var business = await _context.Businesses.FindAsync(businessId);

        if (inviter != null && business != null)
        {
            var emailSent = await _emailService.SendTeamInvitationAsync(
                email,
                inviter.FullName,
                business.Name,
                invitation.Token
            );

            if (emailSent)
            {
                _logger.LogInformation("Team invitation email sent: {InvitationId} for {Email}", invitation.Id, email);
            }
            else
            {
                _logger.LogWarning("Failed to send invitation email: {InvitationId} for {Email}", invitation.Id, email);
            }
        }

        return invitation;
    }

    public async Task<TeamInvitation?> AcceptInvitationAsync(string token, int userId)
    {
        var invitation = await _context.TeamInvitations
            .Include(ti => ti.Business)
            .FirstOrDefaultAsync(ti => ti.Token == token && ti.Status == InvitationStatus.Pending);

        if (invitation == null)
        {
            throw new InvalidOperationException("Invalid or expired invitation");
        }

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await _context.SaveChangesAsync();
            throw new InvalidOperationException("This invitation has expired");
        }

        // Get user email
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Email != invitation.Email)
        {
            throw new InvalidOperationException("This invitation is not for your email address");
        }

        // Check if already a member
        var existingMember = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu => bu.BusinessId == invitation.BusinessId && bu.UserId == userId);

        if (existingMember != null)
        {
            throw new InvalidOperationException("You are already a member of this team");
        }

        // Add user to business
        var businessUser = new BusinessUser
        {
            BusinessId = invitation.BusinessId,
            UserId = userId,
            Role = invitation.Role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.BusinessUsers.Add(businessUser);

        // Update invitation
        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedByUserId = userId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} accepted invitation {InvitationId}", userId, invitation.Id);

        return invitation;
    }

    public async Task<bool> RevokeInvitationAsync(int invitationId, int userId)
    {
        var invitation = await _context.TeamInvitations
            .FirstOrDefaultAsync(ti => ti.Id == invitationId);

        if (invitation == null)
        {
            return false;
        }

        // Check permissions
        if (!await CanManageTeamAsync(invitation.BusinessId, userId))
        {
            throw new UnauthorizedAccessException("You don't have permission to revoke invitations");
        }

        invitation.Status = InvitationStatus.Revoked;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<TeamInvitation>> GetPendingInvitationsAsync(int businessId)
    {
        return await _context.TeamInvitations
            .Include(ti => ti.InvitedBy)
            .Where(ti => ti.BusinessId == businessId && ti.Status == InvitationStatus.Pending)
            .OrderByDescending(ti => ti.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<BusinessUser>> GetTeamMembersAsync(int businessId)
    {
        return await _context.BusinessUsers
            .Include(bu => bu.User)
            .Where(bu => bu.BusinessId == businessId && bu.IsActive)
            .OrderBy(bu => bu.JoinedAt)
            .ToListAsync();
    }

    public async Task<bool> RemoveTeamMemberAsync(int businessId, int userId, int removedByUserId)
    {
        // Check permissions
        if (!await CanManageTeamAsync(businessId, removedByUserId))
        {
            throw new UnauthorizedAccessException("You don't have permission to remove team members");
        }

        // Prevent removing business owner
        var business = await _context.Businesses.FindAsync(businessId);
        if (business?.UserId == userId)
        {
            throw new InvalidOperationException("Cannot remove the business owner");
        }

        var businessUser = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu => bu.BusinessId == businessId && bu.UserId == userId);

        if (businessUser == null)
        {
            return false;
        }

        businessUser.IsActive = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(int businessId, int userId, string newRole, int updatedByUserId)
    {
        // Check permissions
        if (!await CanManageTeamAsync(businessId, updatedByUserId))
        {
            throw new UnauthorizedAccessException("You don't have permission to update member roles");
        }

        // Prevent changing business owner role
        var business = await _context.Businesses.FindAsync(businessId);
        if (business?.UserId == userId)
        {
            throw new InvalidOperationException("Cannot change the business owner's role");
        }

        var businessUser = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu => bu.BusinessId == businessId && bu.UserId == userId);

        if (businessUser == null)
        {
            return false;
        }

        businessUser.Role = newRole;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CanManageTeamAsync(int businessId, int userId)
    {
        // Business owner can always manage
        var business = await _context.Businesses.FindAsync(businessId);
        if (business?.UserId == userId)
        {
            return true;
        }

        // Admins can manage
        var businessUser = await _context.BusinessUsers
            .FirstOrDefaultAsync(bu => bu.BusinessId == businessId && bu.UserId == userId && bu.IsActive);

        return businessUser?.Role == "Admin" || businessUser?.Role == "Owner";
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
