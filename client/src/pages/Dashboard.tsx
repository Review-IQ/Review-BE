import { useState, useEffect, useCallback } from 'react';
import {
  BarChart3, Star, MessageSquare,
  RefreshCw, Bell, ExternalLink,
  Activity, Clock, ThumbsUp, ThumbsDown, Minus, Calendar, Share2, MapPin
} from 'lucide-react';
import { api } from '../services/api';
import { Link } from 'react-router-dom';
import { useLocation } from '../contexts/LocationContext';

interface DashboardStats {
  totalReviews: number;
  averageRating: number;
  unreadReviews: number;
  connectedPlatforms: number;
  smsUsage: {
    sent: number;
    limit: number;
    remaining: number;
  };
  subscriptionPlan: string;
  recentReviews: {
    id: number;
    platform: string;
    reviewerName: string;
    rating: number;
    reviewDate: string;
  }[];
}

interface PlatformBreakdown {
  platform: string;
  totalReviews: number;
  averageRating: number;
  positiveCount: number;
  neutralCount: number;
  negativeCount: number;
}

interface AnalyticsOverview {
  totalReviews: number;
  averageRating: number;
  responseRate: number;
  sentimentBreakdown: {
    positive: number;
    neutral: number;
    negative: number;
  };
  thisMonthReviews: number;
  monthlyChange: number;
}

export function Dashboard() {
  const { selectedLocationId, locations, isLoading: locationsLoading } = useLocation();
  console.log('[Dashboard] selectedLocationId:', selectedLocationId, 'locations:', locations, 'locationsLoading:', locationsLoading);
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [platformBreakdown, setPlatformBreakdown] = useState<PlatformBreakdown[]>([]);
  const [analytics, setAnalytics] = useState<AnalyticsOverview | null>(null);
  const [loading, setLoading] = useState(true);
  const [businessId] = useState<number>(1); // TODO: Get from context/state
  const [refreshing, setRefreshing] = useState(false);

  const loadDashboardData = useCallback(async () => {
    try {
      setLoading(true);
      console.log('[Dashboard loadDashboardData] businessId:', businessId, 'selectedLocationId:', selectedLocationId);

      // Load all dashboard data in parallel (with location filtering)
      const [summaryRes, platformRes, analyticsRes] = await Promise.all([
        api.getDashboardSummary(businessId, selectedLocationId),
        api.getPlatformBreakdown(businessId, selectedLocationId),
        api.getAnalyticsOverview(businessId, selectedLocationId)
      ]);

      // Always set the data, even if undefined (our guards will handle it)
      setStats(summaryRes?.data || {
        totalReviews: 0,
        averageRating: 0,
        unreadReviews: 0,
        connectedPlatforms: 0,
        smsUsage: { sent: 0, limit: 0, remaining: 0 },
        subscriptionPlan: 'Free',
        recentReviews: []
      });
      setPlatformBreakdown(platformRes?.data?.platformBreakdown || []);
      setAnalytics(analyticsRes?.data || {
        totalReviews: 0,
        averageRating: 0,
        responseRate: 0,
        sentimentBreakdown: { positive: 0, neutral: 0, negative: 0 },
        thisMonthReviews: 0,
        monthlyChange: 0
      });
    } catch (error) {
      console.error('Error loading dashboard data:', error);
      // Set default values on error
      setStats({
        totalReviews: 0,
        averageRating: 0,
        unreadReviews: 0,
        connectedPlatforms: 0,
        smsUsage: { sent: 0, limit: 0, remaining: 0 },
        subscriptionPlan: 'Free',
        recentReviews: []
      });
      setAnalytics({
        totalReviews: 0,
        averageRating: 0,
        responseRate: 0,
        sentimentBreakdown: { positive: 0, neutral: 0, negative: 0 },
        thisMonthReviews: 0,
        monthlyChange: 0
      });
    } finally {
      setLoading(false);
    }
  }, [businessId, selectedLocationId]);

  useEffect(() => {
    // Only load dashboard data after locations have finished loading
    if (!locationsLoading) {
      console.log('[Dashboard useEffect] Locations loaded, calling loadDashboardData');
      loadDashboardData();
    }
  }, [loadDashboardData, locationsLoading]);

  const handleRefresh = async () => {
    setRefreshing(true);
    await loadDashboardData();
    setRefreshing(false);
  };

  const getStarColor = (rating: number) => {
    if (rating >= 4.5) return 'text-green-500';
    if (rating >= 3.5) return 'text-yellow-500';
    return 'text-red-500';
  };

  const getTimeSince = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    return `${Math.floor(seconds / 86400)}d ago`;
  };

  const renderStars = (rating: number) => {
    return (
      <div className="flex gap-0.5">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            className={`w-4 h-4 ${
              star <= rating ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'
            }`}
          />
        ))}
      </div>
    );
  };

  const getSentimentPercentage = () => {
    if (!analytics || !analytics.sentimentBreakdown) return { positive: 0, neutral: 0, negative: 0 };
    const total = analytics.sentimentBreakdown.positive + analytics.sentimentBreakdown.neutral + analytics.sentimentBreakdown.negative;
    if (total === 0) return { positive: 0, neutral: 0, negative: 0 };
    return {
      positive: Math.round((analytics.sentimentBreakdown.positive / total) * 100),
      neutral: Math.round((analytics.sentimentBreakdown.neutral / total) * 100),
      negative: Math.round((analytics.sentimentBreakdown.negative / total) * 100),
    };
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (!stats || !analytics) {
    return (
      <div className="flex justify-center items-center h-screen">
        <p className="text-gray-500">Failed to load dashboard data</p>
      </div>
    );
  }

  // Now we're guaranteed stats and analytics exist
  const sentimentPercent = getSentimentPercentage();

  // Safeguard: if sentiment data is still undefined after loading, use defaults
  const safeSentiment = {
    positive: sentimentPercent?.positive || 0,
    neutral: sentimentPercent?.neutral || 0,
    negative: sentimentPercent?.negative || 0
  };

  return (
    <div className="w-full">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
        {/* Multi-Location Info Banner */}
        {locations.length === 0 && (
          <div className="mb-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
            <div className="flex items-start">
              <MapPin className="h-5 w-5 text-blue-600 mt-0.5 mr-3 flex-shrink-0" />
              <div>
                <h3 className="text-sm font-medium text-blue-900">Multi-Location Support Available</h3>
                <p className="mt-1 text-sm text-blue-700">
                  You can now manage multiple business locations. Visit the{' '}
                  <Link to="/locations" className="font-medium underline">
                    Locations page
                  </Link>{' '}
                  to add your first location and start tracking reviews by location.
                </p>
              </div>
            </div>
          </div>
        )}

        {selectedLocationId && (
          <div className="mb-6 bg-green-50 border border-green-200 rounded-lg p-3">
            <div className="flex items-center">
              <MapPin className="h-4 w-4 text-green-600 mr-2" />
              <p className="text-sm text-green-800">
                Showing data for: <span className="font-medium">
                  {locations.find(l => l.id === selectedLocationId)?.name || 'Selected Location'}
                </span>
              </p>
            </div>
          </div>
        )}

        {/* Header with Actions */}
        <div className="flex items-center justify-between mb-6 sm:mb-8">
          <div>
            <h1 className="text-2xl sm:text-3xl font-bold text-gray-900">Dashboard Overview</h1>
            <p className="mt-2 text-sm sm:text-base text-gray-600">
              Monitor your business reputation across all platforms
            </p>
          </div>
          <button
            onClick={handleRefresh}
            disabled={refreshing}
            className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:opacity-50"
          >
            <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>
        </div>

        {/* Key Metrics Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6 mb-6 sm:mb-8">
          {/* Total Reviews */}
          <div className="bg-white rounded-lg shadow-md p-4 sm:p-6 border border-gray-200 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Reviews</p>
                <p className="text-3xl font-bold text-gray-900 mt-2">
                  {(stats.totalReviews || 0).toLocaleString()}
                </p>
                <div className="flex items-center gap-1 mt-2">
                  <span className={`text-xs font-medium ${(analytics.monthlyChange || 0) >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                    {(analytics.monthlyChange || 0) >= 0 ? '+' : ''}{(analytics.monthlyChange || 0)}%
                  </span>
                  <span className="text-xs text-gray-500">this month</span>
                </div>
              </div>
              <div className="w-12 h-12 bg-blue-100 rounded-lg flex items-center justify-center">
                <MessageSquare className="w-6 h-6 text-blue-600" />
              </div>
            </div>
          </div>

          {/* Average Rating */}
          <div className="bg-white rounded-lg shadow-md p-4 sm:p-6 border border-gray-200 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Average Rating</p>
                <div className="flex items-center gap-2 mt-2">
                  <p className={`text-3xl font-bold ${getStarColor(stats.averageRating || 0)}`}>
                    {(stats.averageRating || 0).toFixed(1)}
                  </p>
                  <Star className={`w-6 h-6 ${getStarColor(stats.averageRating || 0)} fill-current`} />
                </div>
                <div className="mt-2">
                  {renderStars(Math.round(stats.averageRating || 0))}
                </div>
              </div>
              <div className="w-12 h-12 bg-yellow-100 rounded-lg flex items-center justify-center">
                <Star className="w-6 h-6 text-yellow-600" />
              </div>
            </div>
          </div>

          {/* Response Rate */}
          <div className="bg-white rounded-lg shadow-md p-4 sm:p-6 border border-gray-200 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Response Rate</p>
                <p className="text-3xl font-bold text-purple-600 mt-2">
                  {(analytics.responseRate || 0)}%
                </p>
                <div className="mt-2 w-full bg-gray-200 rounded-full h-2">
                  <div
                    className="bg-purple-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${(analytics.responseRate || 0)}%` }}
                  ></div>
                </div>
              </div>
              <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center">
                <Activity className="w-6 h-6 text-purple-600" />
              </div>
            </div>
          </div>

          {/* Unread Reviews */}
          <div className="bg-white rounded-lg shadow-md p-4 sm:p-6 border border-gray-200 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Unread Reviews</p>
                <p className="text-3xl font-bold text-orange-600 mt-2">
                  {stats.unreadReviews || 0}
                </p>
                <Link
                  to="/reviews?filter=unread"
                  className="text-sm text-primary-600 hover:text-primary-700 mt-2 inline-flex items-center gap-1"
                >
                  View all <ExternalLink className="w-3 h-3" />
                </Link>
              </div>
              <div className="w-12 h-12 bg-orange-100 rounded-lg flex items-center justify-center">
                <Bell className="w-6 h-6 text-orange-600" />
              </div>
            </div>
          </div>
        </div>

        {/* Performance Summary */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6 sm:mb-8">
          {/* This Month Progress */}
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">This Month</h3>
              <Calendar className="w-5 h-5 text-primary-600" />
            </div>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">New Reviews</span>
                <span className="text-lg font-bold text-gray-900">{analytics.thisMonthReviews || 0}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">Growth</span>
                <span className={`text-lg font-bold ${(analytics.monthlyChange || 0) >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                  {(analytics.monthlyChange || 0) >= 0 ? '+' : ''}{(analytics.monthlyChange || 0)}%
                </span>
              </div>
              <div className="pt-2 border-t border-gray-200">
                <div className="flex justify-between items-center text-xs text-gray-500">
                  <span>Target: 50 reviews</span>
                  <span>{Math.round(((analytics.thisMonthReviews || 0) / 50) * 100)}%</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2 mt-2">
                  <div
                    className="bg-primary-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${Math.min(((analytics.thisMonthReviews || 0) / 50) * 100, 100)}%` }}
                  ></div>
                </div>
              </div>
            </div>
          </div>

          {/* Response Performance */}
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Response Stats</h3>
              <Clock className="w-5 h-5 text-purple-600" />
            </div>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">Response Rate</span>
                <span className="text-lg font-bold text-purple-600">{analytics.responseRate || 0}%</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">Pending</span>
                <span className="text-lg font-bold text-orange-600">{stats.unreadReviews || 0}</span>
              </div>
              <div className="pt-2 border-t border-gray-200">
                <Link
                  to="/reviews?filter=unread"
                  className="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center justify-between"
                >
                  <span>Respond to pending</span>
                  <ExternalLink className="w-4 h-4" />
                </Link>
              </div>
            </div>
          </div>

          {/* Platform Summary */}
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Platforms</h3>
              <Share2 className="w-5 h-5 text-blue-600" />
            </div>
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">Connected</span>
                <span className="text-lg font-bold text-blue-600">{stats.connectedPlatforms || 0}</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600">Plan</span>
                <span className="text-sm font-semibold text-gray-900 px-2 py-1 bg-gray-100 rounded">
                  {stats.subscriptionPlan || 'Free'}
                </span>
              </div>
              <div className="pt-2 border-t border-gray-200">
                <Link
                  to="/integrations"
                  className="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center justify-between"
                >
                  <span>Manage platforms</span>
                  <ExternalLink className="w-4 h-4" />
                </Link>
              </div>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mb-6">
          {/* Sentiment Breakdown */}
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Sentiment Analysis</h2>
            <div className="space-y-4">
              <div>
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <ThumbsUp className="w-4 h-4 text-green-600" />
                    <span className="text-sm font-medium text-gray-700">Positive</span>
                  </div>
                  <span className="text-sm font-bold text-gray-900">
                    {analytics.sentimentBreakdown?.positive || 0} ({safeSentiment.positive}%)
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className="bg-green-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${safeSentiment.positive}%` }}
                  ></div>
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <Minus className="w-4 h-4 text-gray-600" />
                    <span className="text-sm font-medium text-gray-700">Neutral</span>
                  </div>
                  <span className="text-sm font-bold text-gray-900">
                    {analytics.sentimentBreakdown?.neutral || 0} ({safeSentiment.neutral}%)
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className="bg-gray-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${safeSentiment.neutral}%` }}
                  ></div>
                </div>
              </div>

              <div>
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-2">
                    <ThumbsDown className="w-4 h-4 text-red-600" />
                    <span className="text-sm font-medium text-gray-700">Negative</span>
                  </div>
                  <span className="text-sm font-bold text-gray-900">
                    {analytics.sentimentBreakdown?.negative || 0} ({safeSentiment.negative}%)
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className="bg-red-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${safeSentiment.negative}%` }}
                  ></div>
                </div>
              </div>
            </div>
          </div>

          {/* Platform Breakdown */}
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <div className="flex items-center gap-2 mb-4">
              <BarChart3 className="w-5 h-5 text-primary-600" />
              <h2 className="text-lg font-semibold text-gray-900">Platform Breakdown</h2>
            </div>

            <div className="space-y-4">
              {platformBreakdown.length > 0 ? platformBreakdown.map((platform) => (
                <div key={platform.platform} className="border-b border-gray-100 pb-4 last:border-0">
                  <div className="flex items-center justify-between mb-2">
                    <span className="font-medium text-gray-900">{platform.platform || 'Unknown'}</span>
                    <span className="text-sm text-gray-600">{platform.totalReviews || 0} reviews</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <div className="flex-1 bg-gray-200 rounded-full h-2 mr-3">
                      <div
                        className="bg-primary-600 h-2 rounded-full transition-all duration-300"
                        style={{
                          width: `${(stats.totalReviews || 0) > 0 ? ((platform.totalReviews || 0) / (stats.totalReviews || 1)) * 100 : 0}%`
                        }}
                      ></div>
                    </div>
                    <div className="flex items-center gap-1">
                      <Star className={`w-4 h-4 ${getStarColor(platform.averageRating || 0)} fill-current`} />
                      <span className={`text-sm font-medium ${getStarColor(platform.averageRating || 0)}`}>
                        {(platform.averageRating || 0).toFixed(1)}
                      </span>
                    </div>
                  </div>
                </div>
              )) : (
                <p className="text-sm text-gray-500 text-center py-4">No platform data available</p>
              )}
            </div>
          </div>

          {/* Account Info */}
          <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Account Info</h2>
            <div className="space-y-4">
              <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                <span className="text-sm text-gray-600">Subscription Plan</span>
                <span className="text-sm font-bold text-primary-600">{stats.subscriptionPlan || 'Free'}</span>
              </div>
              <div className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                <span className="text-sm text-gray-600">Connected Platforms</span>
                <span className="text-sm font-bold text-gray-900">{stats.connectedPlatforms || 0}</span>
              </div>
              <div className="p-3 bg-gray-50 rounded-lg">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm text-gray-600">SMS Usage</span>
                  <span className="text-sm font-bold text-gray-900">
                    {stats.smsUsage?.sent || 0} / {stats.smsUsage?.limit || 10}
                  </span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div
                    className={`h-2 rounded-full transition-all duration-300 ${
                      ((stats.smsUsage?.sent || 0) / (stats.smsUsage?.limit || 10)) > 0.8 ? 'bg-red-600' : 'bg-primary-600'
                    }`}
                    style={{ width: `${((stats.smsUsage?.sent || 0) / (stats.smsUsage?.limit || 10)) * 100}%` }}
                  ></div>
                </div>
              </div>
              {(stats.subscriptionPlan || 'Free') === 'Free' && (
                <Link
                  to="/subscription"
                  className="block w-full text-center px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
                >
                  Upgrade Plan
                </Link>
              )}
            </div>
          </div>
        </div>

        {/* Recent Activity */}
        <div className="bg-white rounded-lg shadow-md p-6 border border-gray-200">
          <div className="flex items-center justify-between mb-6">
            <div className="flex items-center gap-2">
              <Clock className="w-5 h-5 text-primary-600" />
              <h2 className="text-lg font-semibold text-gray-900">Recent Reviews</h2>
            </div>
            <Link
              to="/reviews"
              className="text-sm text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1"
            >
              View All <ExternalLink className="w-3 h-3" />
            </Link>
          </div>

          <div className="space-y-3">
            {stats.recentReviews && stats.recentReviews.length > 0 ? stats.recentReviews.map((review) => (
              <div
                key={review.id || Math.random()}
                className="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:border-primary-300 hover:bg-primary-50 transition-all"
              >
                <div className="flex items-center gap-4 flex-1">
                  <div className="w-10 h-10 bg-gradient-to-br from-primary-500 to-primary-600 rounded-full flex items-center justify-center text-white font-semibold">
                    {(review.reviewerName || 'A').charAt(0).toUpperCase()}
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium text-gray-900">{review.reviewerName || 'Anonymous'}</span>
                      <span className="text-xs px-2 py-0.5 bg-gray-100 text-gray-700 rounded">
                        {review.platform || 'Unknown'}
                      </span>
                    </div>
                    <div className="flex items-center gap-2">
                      {renderStars(review.rating || 0)}
                      <span className="text-xs text-gray-500">{getTimeSince(review.reviewDate || new Date().toISOString())}</span>
                    </div>
                  </div>
                </div>
                <Link
                  to={`/reviews/${review.id || 0}`}
                  className="px-3 py-1.5 text-sm text-primary-600 hover:bg-primary-100 rounded transition-colors"
                >
                  View
                </Link>
              </div>
            )) : (
              <p className="text-sm text-gray-500 text-center py-8">No recent reviews</p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
