import { useState, useEffect } from 'react';
import { Users, Mail, UserPlus, Trash2, Shield, Crown, UserCheck, X, MapPin } from 'lucide-react';
import { api } from '../services/api';
import { useLocation } from '../contexts/LocationContext';

interface TeamMember {
  id: number;
  userId: number;
  user: {
    id: number;
    fullName: string;
    email: string;
  };
  role: string;
  joinedAt: string;
  isActive: boolean;
}

interface Invitation {
  id: number;
  email: string;
  role: string;
  invitedBy: {
    fullName: string;
  };
  createdAt: string;
  expiresAt: string;
  status: number;
}

interface TeamManagementProps {
  businessId: number;
}

export function TeamManagement({ businessId }: TeamManagementProps) {
  const { locations } = useLocation();
  const [members, setMembers] = useState<TeamMember[]>([]);
  const [invitations, setInvitations] = useState<Invitation[]>([]);
  const [loading, setLoading] = useState(true);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Member');
  const [selectedLocations, setSelectedLocations] = useState<number[]>([]);
  const [inviting, setInviting] = useState(false);
  const [activeSubTab, setActiveSubTab] = useState<'invite' | 'active'>('invite');

  useEffect(() => {
    loadTeamData();
  }, [businessId]);

  const loadTeamData = async () => {
    try {
      setLoading(true);
      const [membersRes, invitationsRes] = await Promise.all([
        api.getTeamMembers(businessId),
        api.getPendingInvitations(businessId)
      ]);
      setMembers(membersRes.data);
      setInvitations(invitationsRes.data);
    } catch (error) {
      console.error('Error loading team data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleInvite = async () => {
    if (!inviteEmail) return;

    try {
      setInviting(true);
      await api.inviteUser(businessId, inviteEmail, inviteRole);
      console.log('Assigning locations:', selectedLocations); // For demo
      setInviteEmail('');
      setInviteRole('Member');
      setSelectedLocations([]);
      await loadTeamData();
      alert('Invitation sent successfully!');
    } catch (error) {
      console.error('Error inviting user:', error);
      alert('Failed to send invitation');
    } finally {
      setInviting(false);
    }
  };

  const toggleLocation = (locationId: number) => {
    setSelectedLocations(prev =>
      prev.includes(locationId)
        ? prev.filter(id => id !== locationId)
        : [...prev, locationId]
    );
  };

  const selectAllLocations = () => {
    setSelectedLocations(locations.map(l => l.id));
  };

  const clearAllLocations = () => {
    setSelectedLocations([]);
  };

  const handleRevokeInvitation = async (invitationId: number) => {
    if (!confirm('Are you sure you want to revoke this invitation?')) return;

    try {
      await api.revokeInvitation(invitationId);
      await loadTeamData();
    } catch (error) {
      console.error('Error revoking invitation:', error);
      alert('Failed to revoke invitation');
    }
  };

  const handleRemoveMember = async (userId: number, memberName: string) => {
    if (!confirm(`Are you sure you want to remove ${memberName} from the team?`)) return;

    try {
      await api.removeTeamMember(businessId, userId);
      await loadTeamData();
    } catch (error) {
      console.error('Error removing team member:', error);
      alert('Failed to remove team member');
    }
  };

  const handleUpdateRole = async (userId: number, newRole: string) => {
    try {
      await api.updateMemberRole(businessId, userId, newRole);
      await loadTeamData();
      alert('Role updated successfully!');
    } catch (error) {
      console.error('Error updating role:', error);
      alert('Failed to update role');
    }
  };

  const getRoleIcon = (role: string) => {
    if (role === 'Owner') return <Crown className="w-4 h-4 text-yellow-600" />;
    if (role === 'Admin') return <Shield className="w-4 h-4 text-blue-600" />;
    return <UserCheck className="w-4 h-4 text-gray-600" />;
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h2 className="text-xl font-semibold text-gray-900">Team Management</h2>
        <p className="text-sm text-gray-600 mt-1">
          Invite team members to collaborate on your business
        </p>
      </div>

      {/* Sub Tabs */}
      <div className="border-b border-gray-200">
        <div className="flex gap-4">
          <button
            onClick={() => setActiveSubTab('invite')}
            className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
              activeSubTab === 'invite'
                ? 'border-primary-600 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <div className="flex items-center gap-2">
              <UserPlus className="w-4 h-4" />
              Invite Users
            </div>
          </button>
          <button
            onClick={() => setActiveSubTab('active')}
            className={`px-4 py-3 text-sm font-medium border-b-2 transition-colors ${
              activeSubTab === 'active'
                ? 'border-primary-600 text-primary-600'
                : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
            }`}
          >
            <div className="flex items-center gap-2">
              <Users className="w-4 h-4" />
              Active Users ({members.length})
            </div>
          </button>
        </div>
      </div>

      {/* Invite Users Tab Content */}
      {activeSubTab === 'invite' && (
        <>
          {/* Invite Form */}
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Invite New Team Member</h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Email Address
                </label>
                <input
                  type="email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="colleague@example.com"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Role
                </label>
                <select
                  value={inviteRole}
                  onChange={(e) => setInviteRole(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                >
                  <option value="Member">Member - Can view and respond to reviews</option>
                  <option value="Admin">Admin - Can manage team and settings</option>
                </select>
              </div>

              {/* Location Access */}
              {locations.length > 0 && (
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <label className="block text-sm font-medium text-gray-700">
                      Location Access
                    </label>
                    <div className="flex gap-2">
                      <button
                        type="button"
                        onClick={selectAllLocations}
                        className="text-xs text-primary-600 hover:text-primary-700"
                      >
                        Select All
                      </button>
                      <span className="text-gray-300">|</span>
                      <button
                        type="button"
                        onClick={clearAllLocations}
                        className="text-xs text-gray-600 hover:text-gray-700"
                      >
                        Clear All
                      </button>
                    </div>
                  </div>
                  <div className="border border-gray-300 rounded-lg p-3 max-h-48 overflow-y-auto">
                    {locations.map((location) => (
                      <label
                        key={location.id}
                        className="flex items-center gap-3 p-2 hover:bg-gray-50 rounded cursor-pointer"
                      >
                        <input
                          type="checkbox"
                          checked={selectedLocations.includes(location.id)}
                          onChange={() => toggleLocation(location.id)}
                          className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                        />
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <MapPin className="w-3 h-3 text-gray-400" />
                            <span className="text-sm text-gray-900">{location.name}</span>
                          </div>
                          {location.city && location.state && (
                            <p className="text-xs text-gray-500 ml-5">
                              {location.city}, {location.state}
                            </p>
                          )}
                        </div>
                      </label>
                    ))}
                  </div>
                  <p className="text-xs text-gray-500 mt-2">
                    {selectedLocations.length === 0
                      ? 'No locations selected - user will have access to all locations'
                      : `${selectedLocations.length} location(s) selected`}
                  </p>
                </div>
              )}

              <button
                onClick={handleInvite}
                disabled={inviting || !inviteEmail}
                className="w-full flex items-center justify-center gap-2 px-6 py-3 bg-blue-600 text-white font-semibold text-base rounded-lg hover:bg-blue-700 focus:ring-4 focus:ring-blue-300 transition-all disabled:bg-gray-300 disabled:text-gray-500 disabled:cursor-not-allowed shadow-lg hover:shadow-xl"
              >
                {inviting ? (
                  <>
                    <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white"></div>
                    Sending Invitation...
                  </>
                ) : (
                  <>
                    <UserPlus className="w-5 h-5" />
                    Send Invitation
                  </>
                )}
              </button>
            </div>
          </div>

          {/* Pending Invitations */}
          {invitations.length > 0 && (
            <div className="bg-white rounded-lg border border-gray-200">
              <div className="px-6 py-4 border-b border-gray-200">
                <h3 className="text-lg font-medium text-gray-900 flex items-center gap-2">
                  <Mail className="w-5 h-5 text-gray-600" />
                  Pending Invitations ({invitations.length})
                </h3>
              </div>
              <div className="divide-y divide-gray-200">
                {invitations.map((invitation) => {
                  // Mock location access for invitations
                  const mockInviteLocationIds = locations.length > 0
                    ? [1, 2].slice(0, Math.floor(Math.random() * 2) + 1)
                    : [];
                  const inviteLocations = locations.filter(l => mockInviteLocationIds.includes(l.id));

                  return (
                    <div key={invitation.id} className="px-6 py-4 hover:bg-gray-50">
                      <div className="flex items-center justify-between mb-2">
                        <div>
                          <p className="text-sm font-medium text-gray-900">{invitation.email}</p>
                          <p className="text-xs text-gray-500">
                            Invited by {invitation.invitedBy.fullName} on {formatDate(invitation.createdAt)}
                          </p>
                          <p className="text-xs text-gray-400">Expires {formatDate(invitation.expiresAt)}</p>
                        </div>
                        <div className="flex items-center gap-3">
                          <span className="px-3 py-1 bg-gray-100 text-gray-700 rounded-full text-sm">
                            {invitation.role}
                          </span>
                          <button
                            onClick={() => handleRevokeInvitation(invitation.id)}
                            className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                            title="Revoke invitation"
                          >
                            <X className="w-4 h-4" />
                          </button>
                        </div>
                      </div>

                      {/* Location Access for Invitations */}
                      {inviteLocations.length > 0 && (
                        <div className="mt-2 flex flex-wrap gap-2">
                          <span className="text-xs text-gray-500 flex items-center gap-1">
                            <MapPin className="w-3 h-3" />
                            Access:
                          </span>
                          {inviteLocations.map((location) => (
                            <span
                              key={location.id}
                              className="inline-flex items-center gap-1 px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs"
                            >
                              {location.name}
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Roles Info */}
          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
            <h4 className="text-sm font-medium text-blue-900 mb-2">Role Permissions</h4>
            <ul className="text-xs text-blue-800 space-y-1">
              <li><strong>Owner:</strong> Full access to all features and settings</li>
              <li><strong>Admin:</strong> Can manage team, invite users, and configure settings</li>
              <li><strong>Member:</strong> Can view reviews, respond to customers, and access analytics</li>
            </ul>
          </div>
        </>
      )}

      {/* Active Users Tab Content */}
      {activeSubTab === 'active' && (
        <div className="bg-white rounded-lg border border-gray-200">
          <div className="px-6 py-4 border-b border-gray-200">
            <h3 className="text-lg font-medium text-gray-900 flex items-center gap-2">
              <Users className="w-5 h-5 text-gray-600" />
              Active Team Members ({members.length})
            </h3>
          </div>
        <div className="divide-y divide-gray-200">
          {members.map((member) => {
            // Mock location access - randomly assign 1-3 locations
            const mockLocationIds = locations.length > 0
              ? [1, 2, 3].slice(0, Math.floor(Math.random() * 3) + 1)
              : [];
            const memberLocations = locations.filter(l => mockLocationIds.includes(l.id));

            return (
            <div key={member.id} className="px-6 py-4 hover:bg-gray-50">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-4">
                  <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
                    <span className="text-sm font-semibold text-primary-700">
                      {member.user.fullName.charAt(0).toUpperCase()}
                    </span>
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-900">{member.user.fullName}</p>
                    <p className="text-xs text-gray-500">{member.user.email}</p>
                    <p className="text-xs text-gray-400 mt-1">Joined {formatDate(member.joinedAt)}</p>
                  </div>
                </div>
              <div className="flex items-center gap-3">
                {member.role === 'Owner' ? (
                  <div className="flex items-center gap-2 px-3 py-1 bg-yellow-100 text-yellow-800 rounded-full">
                    {getRoleIcon(member.role)}
                    <span className="text-sm font-medium">Owner</span>
                  </div>
                ) : (
                  <select
                    value={member.role}
                    onChange={(e) => handleUpdateRole(member.userId, e.target.value)}
                    className="px-3 py-1 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  >
                    <option value="Admin">Admin</option>
                    <option value="Member">Member</option>
                  </select>
                )}
                {member.role !== 'Owner' && (
                  <button
                    onClick={() => handleRemoveMember(member.userId, member.user.fullName)}
                    className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    title="Remove member"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                )}
              </div>
            </div>

            {/* Location Access Display */}
            {memberLocations.length > 0 && (
              <div className="ml-14 mt-2 flex flex-wrap gap-2">
                <span className="text-xs text-gray-500 flex items-center gap-1">
                  <MapPin className="w-3 h-3" />
                  Access:
                </span>
                {memberLocations.map((location) => (
                  <span
                    key={location.id}
                    className="inline-flex items-center gap-1 px-2 py-1 bg-blue-50 text-blue-700 rounded text-xs"
                  >
                    {location.name}
                  </span>
                ))}
              </div>
            )}
            </div>
          );
          })}
        </div>
        </div>
      )}
    </div>
  );
}
