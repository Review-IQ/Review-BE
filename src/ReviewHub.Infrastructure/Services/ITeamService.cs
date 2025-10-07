using ReviewHub.Core.Entities;

namespace ReviewHub.Infrastructure.Services;

public interface ITeamService
{
    Task<TeamInvitation> InviteUserAsync(int businessId, int invitedByUserId, string email, string role);
    Task<TeamInvitation?> AcceptInvitationAsync(string token, int userId);
    Task<bool> RevokeInvitationAsync(int invitationId, int userId);
    Task<List<TeamInvitation>> GetPendingInvitationsAsync(int businessId);
    Task<List<BusinessUser>> GetTeamMembersAsync(int businessId);
    Task<bool> RemoveTeamMemberAsync(int businessId, int userId, int removedByUserId);
    Task<bool> UpdateMemberRoleAsync(int businessId, int userId, string newRole, int updatedByUserId);
    Task<bool> CanManageTeamAsync(int businessId, int userId);
}
