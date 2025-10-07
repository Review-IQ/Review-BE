import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  MessageSquare,
  Link2,
  BarChart3,
  Users,
  Settings,
  Menu,
  X,
  LogOut,
  Bell,
  Sparkles,
  MapPin
} from 'lucide-react';
import { useState, useEffect } from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { useMockAuth0 } from '../auth/MockAuth0Provider';
import { api } from '../services/api';
import LocationSelector from './LocationSelector';

const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === 'true';

export function Navigation() {
  const location = useLocation();
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [unreadCount, setUnreadCount] = useState(0);

  // Use mock auth in demo mode, real Auth0 in production
  const auth0 = IS_DEMO_MODE ? useMockAuth0() : useAuth0();
  const { user, logout, isAuthenticated } = auth0;

  // Fetch unread notification count
  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchUnreadCount = async () => {
      try {
        const { data } = await api.getUnreadNotificationCount();
        setUnreadCount(data.count);
      } catch (error) {
        console.error('Error fetching unread notification count:', error);
      }
    };

    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, 30000); // Poll every 30 seconds
    return () => clearInterval(interval);
  }, [isAuthenticated]);

  const navItems = [
    { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
    { path: '/reviews', label: 'Reviews', icon: MessageSquare },
    { path: '/locations', label: 'Locations', icon: MapPin },
    { path: '/integrations', label: 'Integrations', icon: Link2 },
    { path: '/analytics', label: 'Analytics', icon: BarChart3 },
    { path: '/competitors', label: 'Competitors', icon: Users },
    { path: '/ai-insights', label: 'AI Insights', icon: Sparkles },
    { path: '/pos-automation', label: 'POS Automation', icon: MessageSquare },
    { path: '/settings', label: 'Settings', icon: Settings }
  ];

  const isActive = (path: string) => location.pathname === path;

  const handleLogout = () => {
    logout({ logoutParams: { returnTo: window.location.origin } });
  };

  return (
    <>
      {/* Top Navigation Bar */}
      <nav className="bg-white border-b border-gray-200 fixed top-0 left-0 right-0 z-40">
        <div className="px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            {/* Logo */}
            <div className="flex items-center">
              <Link to="/dashboard" className="flex items-center gap-2">
                <div className="w-8 h-8 bg-primary-600 rounded-lg flex items-center justify-center">
                  <MessageSquare className="w-5 h-5 text-white" />
                </div>
                <span className="text-xl font-bold text-gray-900">ReviewHub</span>
              </Link>
            </div>

            {/* Right Side - Location Selector & User Menu */}
            {isAuthenticated && (
              <div className="flex items-center gap-3">
                <LocationSelector />
                <Link to="/notifications" className="relative p-2 text-gray-600 hover:bg-gray-50 rounded-lg transition-colors">
                  <Bell className="w-5 h-5" />
                  {unreadCount > 0 && (
                    <span className="absolute top-1 right-1 bg-red-500 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">
                      {unreadCount > 9 ? '9+' : unreadCount}
                    </span>
                  )}
                </Link>
                <div className="hidden sm:flex items-center gap-2 px-3 py-2 hover:bg-gray-50 rounded-lg transition-colors">
                  <div className="w-8 h-8 bg-primary-600 rounded-full flex items-center justify-center">
                    {user?.picture ? (
                      <img src={user.picture} alt={user.name} className="w-full h-full rounded-full" />
                    ) : (
                      <span className="text-sm font-semibold text-white">
                        {user?.name?.charAt(0).toUpperCase() || 'U'}
                      </span>
                    )}
                  </div>
                  <div className="hidden lg:block">
                    <p className="text-sm font-medium text-gray-900">{user?.name || 'User'}</p>
                    <p className="text-xs text-gray-500">{user?.email || 'user@example.com'}</p>
                  </div>
                </div>
                <button
                  onClick={handleLogout}
                  className="p-2 text-gray-600 hover:bg-gray-50 rounded-lg transition-colors"
                  title="Logout"
                >
                  <LogOut className="w-5 h-5" />
                </button>
              </div>
            )}

            {/* Mobile Menu Button */}
            <button
              onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
              className="lg:hidden p-2 text-gray-600 hover:bg-gray-50 rounded-lg"
            >
              {isMobileMenuOpen ? (
                <X className="w-6 h-6" />
              ) : (
                <Menu className="w-6 h-6" />
              )}
            </button>
          </div>
        </div>
      </nav>

      {/* Left Sidebar Navigation - Desktop */}
      <aside className="hidden lg:flex fixed left-0 top-16 bottom-0 w-64 bg-white border-r border-gray-200 flex-col z-30">
        <nav className="flex-1 px-4 py-6 space-y-1 overflow-y-auto">
          {navItems.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  isActive(item.path)
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'
                }`}
              >
                <Icon className="w-5 h-5 flex-shrink-0" />
                <span>{item.label}</span>
              </Link>
            );
          })}
        </nav>
      </aside>

      {/* Mobile Sidebar Menu */}
      {isMobileMenuOpen && (
        <>
          {/* Backdrop */}
          <div
            className="lg:hidden fixed inset-0 bg-gray-600 bg-opacity-50 z-40 top-16"
            onClick={() => setIsMobileMenuOpen(false)}
          />

          {/* Sidebar */}
          <aside className="lg:hidden fixed left-0 top-16 bottom-0 w-64 bg-white border-r border-gray-200 flex-col z-50 shadow-xl">
            <nav className="flex-1 px-4 py-6 space-y-1 overflow-y-auto">
              {navItems.map((item) => {
                const Icon = item.icon;
                return (
                  <Link
                    key={item.path}
                    to={item.path}
                    onClick={() => setIsMobileMenuOpen(false)}
                    className={`flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                      isActive(item.path)
                        ? 'bg-primary-50 text-primary-700'
                        : 'text-gray-700 hover:bg-gray-50 hover:text-gray-900'
                    }`}
                  >
                    <Icon className="w-5 h-5 flex-shrink-0" />
                    <span>{item.label}</span>
                  </Link>
                );
              })}
            </nav>
          </aside>
        </>
      )}
    </>
  );
}
