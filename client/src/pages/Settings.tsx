import { useState, useEffect } from 'react';
import {
  Settings as SettingsIcon,
  User,
  Building2,
  Bell,
  CreditCard,
  Shield,
  Mail,
  Phone,
  Save,
  Users,
  Sparkles
} from 'lucide-react';
import { api } from '../services/api';
import { TeamManagement } from '../components/TeamManagement';
import { AISettingsTab } from '../components/AISettingsTab';

interface UserProfile {
  fullName: string;
  email: string;
  phoneNumber: string;
  company: string;
  timezone: string;
}

interface NotificationSettings {
  emailNotifications: boolean;
  pushNotifications: boolean;
  smsNotifications: boolean;
  notifyOnNewReview: boolean;
  notifyOnReviewReply: boolean;
  notifyOnLowRating: boolean;
}

interface BillingInfo {
  plan: string;
  status: string;
  nextBillingDate: string;
  paymentMethod: string;
}

interface AISettings {
  enableAutoReply: boolean;
  autoReplyToPositiveReviews: boolean;
  autoReplyToNeutralReviews: boolean;
  autoReplyToQuestions: boolean;
  enableAISuggestions: boolean;
  enableSentimentAnalysis: boolean;
  enableCompetitorAnalysis: boolean;
  enableInsightsGeneration: boolean;
  responseTone: string;
  responseLength: string;
}

export function Settings() {
  const [activeTab, setActiveTab] = useState<'profile' | 'team' | 'notifications' | 'ai' | 'billing' | 'security'>('profile');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [businessId] = useState(1); // TODO: Get from context/state

  // Profile
  const [profile, setProfile] = useState<UserProfile>({
    fullName: '',
    email: '',
    phoneNumber: '',
    company: '',
    timezone: 'America/New_York'
  });

  // Notifications
  const [notifications, setNotifications] = useState<NotificationSettings>({
    emailNotifications: true,
    pushNotifications: false,
    smsNotifications: false,
    notifyOnNewReview: true,
    notifyOnReviewReply: true,
    notifyOnLowRating: true
  });

  // Billing
  const [billing] = useState<BillingInfo>({
    plan: 'Pro',
    status: 'Active',
    nextBillingDate: '2024-07-01',
    paymentMethod: 'Visa ending in 4242'
  });

  // AI Settings
  const [aiSettings, setAISettings] = useState<AISettings>({
    enableAutoReply: false,
    autoReplyToPositiveReviews: true,
    autoReplyToNeutralReviews: false,
    autoReplyToQuestions: true,
    enableAISuggestions: true,
    enableSentimentAnalysis: true,
    enableCompetitorAnalysis: true,
    enableInsightsGeneration: true,
    responseTone: 'Professional',
    responseLength: 'Medium'
  });

  useEffect(() => {
    loadSettings();
  }, []);

  const loadSettings = async () => {
    try {
      setLoading(true);

      // Load notification preferences
      const { data: notifData } = await api.getNotificationPreferences();
      setNotifications(notifData);

      // Load AI settings
      const { data: aiData } = await api.getAISettings();
      setAISettings(aiData);

      // TODO: Load profile (when API endpoint is ready)
      const mockProfile: UserProfile = {
        fullName: 'John Doe',
        email: 'john@example.com',
        phoneNumber: '+1234567890',
        company: 'My Business Inc.',
        timezone: 'America/New_York'
      };
      setProfile(mockProfile);
    } catch (error) {
      console.error('Error loading settings:', error);
    } finally {
      setLoading(false);
    }
  };

  const saveProfile = async () => {
    try {
      setSaving(true);
      // TODO: Call API to save profile
      console.log('Saving profile:', profile);
      await new Promise(resolve => setTimeout(resolve, 1000));
      alert('Profile updated successfully!');
    } catch (error) {
      console.error('Error saving profile:', error);
      alert('Failed to save profile');
    } finally {
      setSaving(false);
    }
  };

  const saveNotifications = async () => {
    try {
      setSaving(true);
      await api.updateNotificationPreferences(notifications);
      alert('Notification settings updated successfully!');
    } catch (error) {
      console.error('Error saving notifications:', error);
      alert('Failed to save notification settings');
    } finally {
      setSaving(false);
    }
  };

  const saveAISettings = async () => {
    try {
      setSaving(true);
      await api.updateAISettings(aiSettings);
      alert('AI settings updated successfully!');
    } catch (error) {
      console.error('Error saving AI settings:', error);
      alert('Failed to save AI settings');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-3">
          <SettingsIcon className="w-8 h-8 text-primary-600" />
          Settings
        </h1>
        <p className="mt-2 text-gray-600">
          Manage your account settings and preferences
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Sidebar */}
        <div className="lg:col-span-1">
          <div className="bg-white rounded-lg shadow-md border border-gray-200 p-4">
            <nav className="space-y-1">
              <button
                onClick={() => setActiveTab('profile')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  activeTab === 'profile'
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <User className="w-5 h-5" />
                Profile
              </button>
              <button
                onClick={() => setActiveTab('team')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  activeTab === 'team'
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <Users className="w-5 h-5" />
                Team
              </button>
              <button
                onClick={() => setActiveTab('notifications')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  activeTab === 'notifications'
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <Bell className="w-5 h-5" />
                Notifications
              </button>
              <button
                onClick={() => setActiveTab('ai')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  activeTab === 'ai'
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <Sparkles className="w-5 h-5" />
                AI Features
              </button>
              <button
                onClick={() => setActiveTab('billing')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  activeTab === 'billing'
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <CreditCard className="w-5 h-5" />
                Billing
              </button>
              <button
                onClick={() => setActiveTab('security')}
                className={`w-full flex items-center gap-3 px-4 py-3 rounded-lg text-sm font-medium transition-colors ${
                  activeTab === 'security'
                    ? 'bg-primary-50 text-primary-700'
                    : 'text-gray-700 hover:bg-gray-50'
                }`}
              >
                <Shield className="w-5 h-5" />
                Security
              </button>
            </nav>
          </div>
        </div>

        {/* Content */}
        <div className="lg:col-span-3">
          {/* Profile Tab */}
          {activeTab === 'profile' && (
            <div className="bg-white rounded-lg shadow-md border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Profile Information</h2>

              <div className="space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Full Name
                    </label>
                    <div className="relative">
                      <User className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="text"
                        value={profile.fullName}
                        onChange={(e) => setProfile({ ...profile, fullName: e.target.value })}
                        className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Email Address
                    </label>
                    <div className="relative">
                      <Mail className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="email"
                        value={profile.email}
                        onChange={(e) => setProfile({ ...profile, email: e.target.value })}
                        className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Phone Number
                    </label>
                    <div className="relative">
                      <Phone className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="tel"
                        value={profile.phoneNumber}
                        onChange={(e) => setProfile({ ...profile, phoneNumber: e.target.value })}
                        className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Company Name
                    </label>
                    <div className="relative">
                      <Building2 className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                      <input
                        type="text"
                        value={profile.company}
                        onChange={(e) => setProfile({ ...profile, company: e.target.value })}
                        className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Timezone
                    </label>
                    <select
                      value={profile.timezone}
                      onChange={(e) => setProfile({ ...profile, timezone: e.target.value })}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                    >
                      <option value="America/New_York">Eastern Time (ET)</option>
                      <option value="America/Chicago">Central Time (CT)</option>
                      <option value="America/Denver">Mountain Time (MT)</option>
                      <option value="America/Los_Angeles">Pacific Time (PT)</option>
                      <option value="UTC">UTC</option>
                    </select>
                  </div>
                </div>

                <div className="flex justify-end pt-4 border-t border-gray-200">
                  <button
                    onClick={saveProfile}
                    disabled={saving}
                    className="btn-primary flex items-center gap-2"
                  >
                    <Save className="w-4 h-4" />
                    {saving ? 'Saving...' : 'Save Changes'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Team Tab */}
          {activeTab === 'team' && (
            <div className="bg-white rounded-lg shadow-md border border-gray-200 p-6">
              <TeamManagement businessId={businessId} />
            </div>
          )}

          {/* AI Tab */}
          {activeTab === 'ai' && (
            <AISettingsTab
              settings={aiSettings}
              onUpdate={setAISettings}
              onSave={saveAISettings}
              saving={saving}
            />
          )}

          {/* Notifications Tab */}
          {activeTab === 'notifications' && (
            <div className="bg-white rounded-lg shadow-md border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Notification Preferences</h2>

              <div className="space-y-6">
                {/* Notification Channels */}
                <div>
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Notification Channels</h3>
                  <div className="space-y-4">
                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={notifications.emailNotifications}
                        onChange={(e) => setNotifications({ ...notifications, emailNotifications: e.target.checked })}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div>
                        <p className="text-sm font-medium text-gray-900">Email Notifications</p>
                        <p className="text-sm text-gray-500">Receive notifications via email</p>
                      </div>
                    </label>

                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={notifications.pushNotifications}
                        onChange={(e) => setNotifications({ ...notifications, pushNotifications: e.target.checked })}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div>
                        <p className="text-sm font-medium text-gray-900">Push Notifications</p>
                        <p className="text-sm text-gray-500">Receive browser push notifications</p>
                      </div>
                    </label>

                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={notifications.smsNotifications}
                        onChange={(e) => setNotifications({ ...notifications, smsNotifications: e.target.checked })}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div>
                        <p className="text-sm font-medium text-gray-900">SMS Notifications</p>
                        <p className="text-sm text-gray-500">Receive notifications via text message</p>
                      </div>
                    </label>
                  </div>
                </div>

                {/* Notification Types */}
                <div className="pt-6 border-t border-gray-200">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">What to Notify</h3>
                  <div className="space-y-4">
                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={notifications.notifyOnNewReview}
                        onChange={(e) => setNotifications({ ...notifications, notifyOnNewReview: e.target.checked })}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div>
                        <p className="text-sm font-medium text-gray-900">New Reviews</p>
                        <p className="text-sm text-gray-500">Get notified when you receive a new review</p>
                      </div>
                    </label>

                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={notifications.notifyOnReviewReply}
                        onChange={(e) => setNotifications({ ...notifications, notifyOnReviewReply: e.target.checked })}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div>
                        <p className="text-sm font-medium text-gray-900">Review Replies</p>
                        <p className="text-sm text-gray-500">Get notified when someone replies to your review response</p>
                      </div>
                    </label>

                    <label className="flex items-center gap-3 cursor-pointer">
                      <input
                        type="checkbox"
                        checked={notifications.notifyOnLowRating}
                        onChange={(e) => setNotifications({ ...notifications, notifyOnLowRating: e.target.checked })}
                        className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
                      />
                      <div>
                        <p className="text-sm font-medium text-gray-900">Low Rating Alerts</p>
                        <p className="text-sm text-gray-500">Get immediate alerts for reviews with 2 stars or less</p>
                      </div>
                    </label>
                  </div>
                </div>

                <div className="flex justify-end pt-4 border-t border-gray-200">
                  <button
                    onClick={saveNotifications}
                    disabled={saving}
                    className="btn-primary flex items-center gap-2"
                  >
                    <Save className="w-4 h-4" />
                    {saving ? 'Saving...' : 'Save Preferences'}
                  </button>
                </div>
              </div>
            </div>
          )}

          {/* Billing Tab */}
          {activeTab === 'billing' && (
            <div className="bg-white rounded-lg shadow-md border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Billing & Subscription</h2>

              <div className="space-y-6">
                {/* Current Plan */}
                <div className="bg-gradient-to-r from-primary-50 to-blue-50 rounded-lg p-6 border border-primary-200">
                  <div className="flex items-center justify-between mb-4">
                    <div>
                      <h3 className="text-lg font-semibold text-gray-900">{billing.plan} Plan</h3>
                      <p className="text-sm text-gray-600">Status: {billing.status}</p>
                    </div>
                    <span className="px-4 py-2 bg-primary-600 text-white rounded-lg font-semibold">
                      $49/month
                    </span>
                  </div>
                  <p className="text-sm text-gray-700">
                    Next billing date: {new Date(billing.nextBillingDate).toLocaleDateString()}
                  </p>
                </div>

                {/* Payment Method */}
                <div>
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Payment Method</h3>
                  <div className="flex items-center justify-between p-4 border border-gray-200 rounded-lg">
                    <div className="flex items-center gap-3">
                      <CreditCard className="w-8 h-8 text-gray-600" />
                      <div>
                        <p className="text-sm font-medium text-gray-900">{billing.paymentMethod}</p>
                        <p className="text-xs text-gray-500">Expires 12/2025</p>
                      </div>
                    </div>
                    <button className="text-sm text-primary-600 hover:text-primary-700 font-medium">
                      Update
                    </button>
                  </div>
                </div>

                {/* Available Plans */}
                <div className="pt-6 border-t border-gray-200">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Available Plans</h3>
                  <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="border border-gray-200 rounded-lg p-4">
                      <h4 className="font-semibold text-gray-900 mb-2">Free</h4>
                      <p className="text-2xl font-bold text-gray-900 mb-4">$0<span className="text-sm font-normal text-gray-600">/mo</span></p>
                      <ul className="space-y-2 text-sm text-gray-600 mb-4">
                        <li>• 1 Business</li>
                        <li>• 100 Reviews/month</li>
                        <li>• Basic Analytics</li>
                      </ul>
                      <button className="w-full px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200">
                        Downgrade
                      </button>
                    </div>

                    <div className="border-2 border-primary-600 rounded-lg p-4 relative">
                      <span className="absolute -top-3 left-1/2 transform -translate-x-1/2 px-3 py-1 bg-primary-600 text-white text-xs rounded-full">
                        Current
                      </span>
                      <h4 className="font-semibold text-gray-900 mb-2">Pro</h4>
                      <p className="text-2xl font-bold text-gray-900 mb-4">$49<span className="text-sm font-normal text-gray-600">/mo</span></p>
                      <ul className="space-y-2 text-sm text-gray-600 mb-4">
                        <li>• 5 Businesses</li>
                        <li>• Unlimited Reviews</li>
                        <li>• Advanced Analytics</li>
                        <li>• AI Reply Suggestions</li>
                      </ul>
                      <button className="w-full btn-primary" disabled>
                        Current Plan
                      </button>
                    </div>

                    <div className="border border-gray-200 rounded-lg p-4">
                      <h4 className="font-semibold text-gray-900 mb-2">Enterprise</h4>
                      <p className="text-2xl font-bold text-gray-900 mb-4">$199<span className="text-sm font-normal text-gray-600">/mo</span></p>
                      <ul className="space-y-2 text-sm text-gray-600 mb-4">
                        <li>• Unlimited Businesses</li>
                        <li>• Unlimited Reviews</li>
                        <li>• Custom Integrations</li>
                        <li>• Priority Support</li>
                      </ul>
                      <button className="w-full btn-primary">
                        Upgrade
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Security Tab */}
          {activeTab === 'security' && (
            <div className="bg-white rounded-lg shadow-md border border-gray-200 p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-6">Security Settings</h2>

              <div className="space-y-6">
                <div>
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Password</h3>
                  <p className="text-sm text-gray-600 mb-4">
                    Your account is secured with Auth0. To change your password, please use the Auth0 password reset.
                  </p>
                  <button className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200">
                    Change Password
                  </button>
                </div>

                <div className="pt-6 border-t border-gray-200">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Two-Factor Authentication</h3>
                  <p className="text-sm text-gray-600 mb-4">
                    Add an extra layer of security to your account
                  </p>
                  <button className="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700">
                    Enable 2FA
                  </button>
                </div>

                <div className="pt-6 border-t border-gray-200">
                  <h3 className="text-lg font-medium text-gray-900 mb-4">Active Sessions</h3>
                  <div className="space-y-3">
                    <div className="flex items-center justify-between p-4 border border-gray-200 rounded-lg">
                      <div>
                        <p className="text-sm font-medium text-gray-900">Current Session</p>
                        <p className="text-xs text-gray-500">Chrome on Windows • Active now</p>
                      </div>
                      <span className="px-2 py-1 bg-green-100 text-green-800 text-xs rounded">Active</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
