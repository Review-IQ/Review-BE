import { useState, useEffect } from 'react';
import { Link2, CheckCircle, RefreshCw, Trash2, ExternalLink } from 'lucide-react';
import { useLocation } from '../contexts/LocationContext';

interface Platform {
  id: number;
  name: string;
  displayName: string;
  description: string;
  icon: string;
  supportsOAuth: boolean;
  isComingSoon: boolean;
}

interface Connection {
  id: number;
  platform: string;
  platformId: number;
  platformBusinessId: string;
  platformBusinessName: string;
  connectedAt: string;
  lastSyncedAt: string | null;
  isActive: boolean;
  autoSync: boolean;
}

export function Integrations() {
  const { selectedLocationId } = useLocation();
  const [platforms, setPlatforms] = useState<Platform[]>([]);
  const [connections, setConnections] = useState<Connection[]>([]);
  const [selectedBusinessId] = useState(1); // TODO: Get from context/dropdown
  const [loading, setLoading] = useState(true);
  const [syncing, setSyncing] = useState<number | null>(null);

  useEffect(() => {
    loadPlatforms();
    loadConnections();
  }, [selectedBusinessId, selectedLocationId]);

  const loadPlatforms = async () => {
    try {
      // TODO: Replace with actual API call
      const mockPlatforms: Platform[] = [
        {
          id: 1,
          name: 'Google',
          displayName: 'Google Business Profile',
          description: 'Connect your Google Business Profile to manage reviews and respond to customers',
          icon: 'google',
          supportsOAuth: true,
          isComingSoon: false
        },
        {
          id: 2,
          name: 'Yelp',
          displayName: 'Yelp',
          description: 'Sync Yelp reviews and respond to customer feedback',
          icon: 'yelp',
          supportsOAuth: true,
          isComingSoon: false
        },
        {
          id: 3,
          name: 'Facebook',
          displayName: 'Facebook',
          description: 'Manage Facebook page reviews and ratings',
          icon: 'facebook',
          supportsOAuth: true,
          isComingSoon: false
        },
        {
          id: 4,
          name: 'TripAdvisor',
          displayName: 'TripAdvisor',
          description: 'Track and respond to TripAdvisor reviews',
          icon: 'plane',
          supportsOAuth: false,
          isComingSoon: true
        },
        {
          id: 5,
          name: 'Zomato',
          displayName: 'Zomato',
          description: 'Connect Zomato restaurant reviews',
          icon: 'utensils',
          supportsOAuth: false,
          isComingSoon: true
        }
      ];

      setPlatforms(mockPlatforms);
    } catch (error) {
      console.error('Error loading platforms:', error);
    }
  };

  const loadConnections = async () => {
    try {
      setLoading(true);
      // TODO: Replace with actual API call
      const mockConnections: Connection[] = [
        {
          id: 1,
          platform: 'Google',
          platformId: 1,
          platformBusinessId: 'ChIJN1t_tDeuEmsRUsoyG83frY4',
          platformBusinessName: 'My Business on Google',
          connectedAt: new Date().toISOString(),
          lastSyncedAt: new Date().toISOString(),
          isActive: true,
          autoSync: true
        }
      ];

      setConnections(mockConnections);
    } catch (error) {
      console.error('Error loading connections:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleConnect = async (platform: Platform) => {
    try {
      // TODO: Call API to initiate OAuth
      alert(`Connecting to ${platform.displayName}... (OAuth integration coming soon)`);
    } catch (error) {
      console.error('Error connecting platform:', error);
    }
  };

  const handleSync = async (connectionId: number) => {
    try {
      setSyncing(connectionId);
      // TODO: Call API to sync
      await new Promise(resolve => setTimeout(resolve, 2000));
      await loadConnections();
    } catch (error) {
      console.error('Error syncing:', error);
    } finally {
      setSyncing(null);
    }
  };

  const handleDisconnect = async (_connectionId: number, platformName: string) => {
    if (!confirm(`Are you sure you want to disconnect ${platformName}?`)) {
      return;
    }

    try {
      // TODO: Call API to disconnect
      await loadConnections();
    } catch (error) {
      console.error('Error disconnecting:', error);
    }
  };

  const isConnected = (platformId: number) => {
    return connections.some(c => c.platformId === platformId && c.isActive);
  };

  const getConnection = (platformId: number) => {
    return connections.find(c => c.platformId === platformId && c.isActive);
  };

  const getTimeSince = (dateString: string | null) => {
    if (!dateString) return 'Never';
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)} minutes ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)} hours ago`;
    return `${Math.floor(seconds / 86400)} days ago`;
  };

  return (
    <div className="px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-3">
          <Link2 className="w-8 h-8 text-primary-600" />
          Platform Integrations
        </h1>
        <p className="mt-2 text-gray-600">
          Connect your review platforms to manage all your reviews in one place
        </p>
      </div>

      {loading ? (
        <div className="flex justify-center items-center h-64">
          <RefreshCw className="w-8 h-8 text-primary-600 animate-spin" />
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {platforms.map((platform) => {
            const connection = getConnection(platform.id);
            const connected = isConnected(platform.id);

            return (
              <div
                key={platform.id}
                className="bg-white rounded-lg shadow-md overflow-hidden border border-gray-200 hover:shadow-lg transition-shadow"
              >
                {/* Platform Header */}
                <div className="p-6 border-b border-gray-100">
                  <div className="flex items-start justify-between">
                    <div className="flex items-center gap-3">
                      <div className="w-12 h-12 bg-primary-100 rounded-lg flex items-center justify-center">
                        <span className="text-2xl">
                          {platform.name === 'Google' && '🔴'}
                          {platform.name === 'Yelp' && '⭐'}
                          {platform.name === 'Facebook' && '📘'}
                          {platform.name === 'TripAdvisor' && '✈️'}
                          {platform.name === 'Zomato' && '🍽️'}
                        </span>
                      </div>
                      <div>
                        <h3 className="text-lg font-semibold text-gray-900">
                          {platform.displayName}
                        </h3>
                        {connected && (
                          <span className="inline-flex items-center gap-1 text-sm text-green-600 font-medium">
                            <CheckCircle className="w-4 h-4" />
                            Connected
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  <p className="mt-3 text-sm text-gray-600">
                    {platform.description}
                  </p>
                </div>

                {/* Connection Details or Actions */}
                <div className="p-6 bg-gray-50">
                  {connected && connection ? (
                    <>
                      <div className="space-y-2 mb-4">
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Business:</span>
                          <span className="font-medium text-gray-900">
                            {connection.platformBusinessName}
                          </span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Connected:</span>
                          <span className="text-gray-900">
                            {new Date(connection.connectedAt).toLocaleDateString()}
                          </span>
                        </div>
                        <div className="flex justify-between text-sm">
                          <span className="text-gray-600">Last Synced:</span>
                          <span className="text-gray-900">
                            {getTimeSince(connection.lastSyncedAt)}
                          </span>
                        </div>
                      </div>

                      <div className="flex gap-2">
                        <button
                          onClick={() => handleSync(connection.id)}
                          disabled={syncing === connection.id}
                          className="flex-1 btn-primary flex items-center justify-center gap-2"
                        >
                          <RefreshCw className={`w-4 h-4 ${syncing === connection.id ? 'animate-spin' : ''}`} />
                          {syncing === connection.id ? 'Syncing...' : 'Sync Now'}
                        </button>
                        <button
                          onClick={() => handleDisconnect(connection.id, platform.displayName)}
                          className="px-4 py-2 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 transition-colors"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </>
                  ) : (
                    <>
                      {platform.isComingSoon ? (
                        <div className="text-center py-2">
                          <span className="inline-flex items-center px-3 py-1 rounded-full text-sm font-medium bg-yellow-100 text-yellow-800">
                            Coming Soon
                          </span>
                        </div>
                      ) : platform.supportsOAuth ? (
                        <button
                          onClick={() => handleConnect(platform)}
                          className="w-full btn-primary flex items-center justify-center gap-2"
                        >
                          <ExternalLink className="w-4 h-4" />
                          Connect {platform.name}
                        </button>
                      ) : (
                        <button
                          disabled
                          className="w-full px-4 py-2 bg-gray-200 text-gray-500 rounded-lg cursor-not-allowed"
                        >
                          Manual Setup Required
                        </button>
                      )}
                    </>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Stats Summary */}
      {connections.length > 0 && (
        <div className="mt-8 bg-blue-50 rounded-lg p-6 border border-blue-100">
          <h3 className="text-lg font-semibold text-blue-900 mb-2">
            Integration Summary
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="flex items-center gap-3">
              <CheckCircle className="w-6 h-6 text-blue-600" />
              <div>
                <p className="text-sm text-blue-600">Connected Platforms</p>
                <p className="text-2xl font-bold text-blue-900">{connections.length}</p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <RefreshCw className="w-6 h-6 text-blue-600" />
              <div>
                <p className="text-sm text-blue-600">Auto-Sync Enabled</p>
                <p className="text-2xl font-bold text-blue-900">
                  {connections.filter(c => c.autoSync).length}
                </p>
              </div>
            </div>
            <div className="flex items-center gap-3">
              <Link2 className="w-6 h-6 text-blue-600" />
              <div>
                <p className="text-sm text-blue-600">Available Platforms</p>
                <p className="text-2xl font-bold text-blue-900">{platforms.length}</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
