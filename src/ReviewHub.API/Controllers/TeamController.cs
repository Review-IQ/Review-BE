using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReviewHub.Infrastructure.Services;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly ITeamService _teamService;
    private readonly IAuth0Service _auth0Service;
    private readonly ILogger<TeamController> _logger;

    public TeamController(
        ITeamService teamService,
        IAuth0Service auth0Service,
        ILogger<TeamController> logger)
    {
        _teamService = teamService;
        _auth0Service = auth0Service;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get all team members for a business
    /// </summary>
    [HttpGet("{businessId}/members")]
    public async Task<IActionResult> GetTeamMembers(int businessId)
    {
        try
        {
            var members = await _teamService.GetTeamMembersAsync(businessId);
            return Ok(members);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team members for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "Error retrieving team members" });
        }
    }

    /// <summary>
    /// Get pending invitations for a business
    /// </summary>
    [HttpGet("{businessId}/invitations")]
    public async Task<IActionResult> GetPendingInvitations(int businessId)
    {
        try
        {
            var invitations = await _teamService.GetPendingInvitationsAsync(businessId);
            return Ok(invitations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invitations for business {BusinessId}", businessId);
            return StatusCode(500, new { message = "Error retrieving invitations" });
        }
    }

    /// <summary>
    /// Invite a user to join a business
    /// </summary>
    [HttpPost("{businessId}/invite")]
    public async Task<IActionResult> InviteUser(int businessId, [FromBody] InviteUserRequest request)
    {
        try
        {
            var userId = GetUserId();
            var invitation = await _teamService.InviteUserAsync(businessId, userId, request.Email, request.Role);

            return Ok(new
            {
                message = "Invitation sent successfully",
                invitation = new
                {
                    invitation.Id,
                    invitation.Email,
                    invitation.Role,
                    invitation.CreatedAt,
                    invitation.ExpiresAt
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting user to business {BusinessId}", businessId);
            return StatusCode(500, new { message = "Error sending invitation" });
        }
    }

    /// <summary>
    /// Accept a team invitation
    /// </summary>
    [HttpPost("accept/{token}")]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        try
        {
            var userId = GetUserId();
            var invitation = await _teamService.AcceptInvitationAsync(token, userId);

            return Ok(new { message = "Invitation accepted successfully", invitation });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation");
            return StatusCode(500, new { message = "Error accepting invitation" });
        }
    }

    /// <summary>
    /// Revoke a pending invitation
    /// </summary>
    [HttpDelete("invitations/{invitationId}")]
    public async Task<IActionResult> RevokeInvitation(int invitationId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _teamService.RevokeInvitationAsync(invitationId, userId);

            if (!result)
            {
                return NotFound(new { message = "Invitation not found" });
            }

            return Ok(new { message = "Invitation revoked successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation {InvitationId}", invitationId);
            return StatusCode(500, new { message = "Error revoking invitation" });
        }
    }

    /// <summary>
    /// Remove a team member from a business
    /// </summary>
    [HttpDelete("{businessId}/members/{userId}")]
    public async Task<IActionResult> RemoveTeamMember(int businessId, int userId)
    {
        try
        {
            var currentUserId = GetUserId();
            var result = await _teamService.RemoveTeamMemberAsync(businessId, userId, currentUserId);

            if (!result)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(new { message = "Team member removed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing team member");
            return StatusCode(500, new { message = "Error removing team member" });
        }
    }

    /// <summary>
    /// Update a team member's role
    /// </summary>
    [HttpPut("{businessId}/members/{userId}/role")]
    public async Task<IActionResult> UpdateMemberRole(int businessId, int userId, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var currentUserId = GetUserId();
            var result = await _teamService.UpdateMemberRoleAsync(businessId, userId, request.Role, currentUserId);

            if (!result)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(new { message = "Role updated successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating member role");
            return StatusCode(500, new { message = "Error updating role" });
        }
    }

    // TODO: Implement these endpoints when TeamService methods are ready
    // /// <summary>
    // /// Get invitation details (public endpoint - no auth required)
    // /// </summary>
    // [HttpGet("invitation/{token}/details")]
    // [AllowAnonymous]
    // public async Task<IActionResult> GetInvitationDetails(string token)
    // {
    //     try
    //     {
    //         var details = await _teamService.GetInvitationDetailsAsync(token);
    //         if (details == null)
    //         {
    //             return NotFound(new { message = "Invalid or expired invitation" });
    //         }

    //         return Ok(details);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error getting invitation details for token {Token}", token);
    //         return StatusCode(500, new { message = "Error retrieving invitation details" });
    //     }
    // }

    // /// <summary>
    // /// Accept invitation and register new user (public endpoint - no auth required)
    // /// </summary>
    // [HttpPost("invitation/{token}/accept-and-register")]
    // [AllowAnonymous]
    // public async Task<IActionResult> AcceptInvitationAndRegister(string token, [FromBody] RegisterUserRequest request)
    // {
    //     try
    //     {
    //         // 1. Get invitation details
    //         var invitationDetails = await _teamService.GetInvitationDetailsAsync(token);
    //         if (invitationDetails == null)
    //         {
    //             return NotFound(new { message = "Invalid or expired invitation" });
    //         }

    //         // 2. Create user in Auth0 with MFA enforced
    //         var auth0UserId = await _auth0Service.CreateUserAsync(
    //             invitationDetails.Email,
    //             request.Password,
    //             request.FullName,
    //             request.PhoneNumber
    //         );

    //         // 3. Accept invitation (creates user in our database)
    //         var result = await _teamService.AcceptInvitationAndRegisterAsync(token, new TeamService.AcceptInvitationRequest
    //         {
    //             FullName = request.FullName,
    //             PhoneNumber = request.PhoneNumber,
    //             Auth0UserId = auth0UserId
    //         });

    //         return Ok(new { message = "Registration successful. Please check your email to verify your account.", userId = result.UserId });
    //     }
    //     catch (InvalidOperationException ex)
    //     {
    //         return BadRequest(new { message = ex.Message });
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError(ex, "Error accepting invitation and registering user");
    //         return StatusCode(500, new { message = "Error completing registration" });
    //     }
    // }
}

public record InviteUserRequest(string Email, string Role);
public record UpdateRoleRequest(string Role);
public record RegisterUserRequest(string FullName, string Password, string? PhoneNumber);
