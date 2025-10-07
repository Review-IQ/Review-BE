import { useState, useEffect } from 'react';
import {
  Users,
  Star,
  TrendingUp,
  TrendingDown,
  Search,
  Plus,
  Trash2,
  ExternalLink,
  Trophy,
  Target,
  AlertCircle,
  RefreshCw,
  Crown,
  Award,
  MessageSquare,
  ThumbsUp,
  Activity
} from 'lucide-react';
import {
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  LineChart,
  Line
} from 'recharts';
import { api } from '../services/api';
import { useLocation } from '../contexts/LocationContext';

interface Competitor {
  id: number;
  name: string;
  platform: string;
  platformUrl: string;
  totalReviews: number;
  avgRating: number;
  ratingTrend: number;
  lastUpdated: string;
  responseRate: number;
  avgResponseTime: number;
  sentiment: {
    positive: number;
    neutral: number;
    negative: number;
  };
  reviewDistribution: {
    rating: number;
    count: number;
  }[];
  recentReviewCount: number;
}

interface MyBusiness {
  name: string;
  avgRating: number;
  totalReviews: number;
  responseRate: number;
  avgResponseTime: number;
  ratingTrend: number;
  sentiment: {
    positive: number;
    neutral: number;
    negative: number;
  };
  recentReviewCount: number;
}

export function Competitors() {
  const { selectedLocationId } = useLocation();
  const [competitors, setCompetitors] = useState<Competitor[]>([]);
  const [myBusiness, setMyBusiness] = useState<MyBusiness | null>(null);
  const [loading, setLoading] = useState(true);
  const [showAddModal, setShowAddModal] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [newCompetitorName, setNewCompetitorName] = useState('');
  const [newCompetitorPlatform, setNewCompetitorPlatform] = useState('Google');
  const [newCompetitorUrl, setNewCompetitorUrl] = useState('');
  const [selectedView, setSelectedView] = useState<'overview' | 'detailed'>('overview');

  useEffect(() => {
    loadData();
  }, [selectedLocationId]);

  const loadData = async () => {
    try {
      setLoading(true);

      // Get business ID from localStorage or use default
      const businessId = 1; // TODO: Get from context or props

      // Fetch data from API (will use mock in demo mode)
      const [competitorsRes, analyticsRes] = await Promise.all([
        api.getCompetitors(businessId, selectedLocationId),
        api.getAnalyticsOverview(businessId, selectedLocationId)
      ]);

      // Set My Business Data from analytics
      if (analyticsRes?.data) {
        const mockMyBusiness: MyBusiness = {
          name: 'My Restaurant',
          avgRating: analyticsRes.data.averageRating || 4.5,
          totalReviews: analyticsRes.data.totalReviews || 1247,
          responseRate: analyticsRes.data.responseRate || 87.3,
          avgResponseTime: 4.2,
          ratingTrend: analyticsRes.data.monthlyChange || 3.5,
          sentiment: {
            positive: Math.round((analyticsRes.data.sentimentBreakdown?.positive /
              (analyticsRes.data.sentimentBreakdown?.positive +
               analyticsRes.data.sentimentBreakdown?.neutral +
               analyticsRes.data.sentimentBreakdown?.negative) * 100)) || 75,
            neutral: Math.round((analyticsRes.data.sentimentBreakdown?.neutral /
              (analyticsRes.data.sentimentBreakdown?.positive +
               analyticsRes.data.sentimentBreakdown?.neutral +
               analyticsRes.data.sentimentBreakdown?.negative) * 100)) || 18,
            negative: Math.round((analyticsRes.data.sentimentBreakdown?.negative /
              (analyticsRes.data.sentimentBreakdown?.positive +
               analyticsRes.data.sentimentBreakdown?.neutral +
               analyticsRes.data.sentimentBreakdown?.negative) * 100)) || 7
          },
          recentReviewCount: analyticsRes.data.thisMonthReviews || 145
        };
        setMyBusiness(mockMyBusiness);
      }

      // Set Competitors Data
      if (competitorsRes?.data) {
        setCompetitors(competitorsRes.data);
      }
    } catch (error) {
      console.error('Error loading data:', error);
    } finally {
      setLoading(false);
    }
  };

  const addCompetitor = async () => {
    if (!newCompetitorName || !newCompetitorUrl) {
      alert('Please fill in all fields');
      return;
    }

    try {
      const businessId = 1; // TODO: Get from context or props

      await api.addCompetitor({
        businessId,
        name: newCompetitorName,
        platform: newCompetitorPlatform,
        platformBusinessId: newCompetitorUrl
      });

      setShowAddModal(false);
      setNewCompetitorName('');
      setNewCompetitorPlatform('Google');
      setNewCompetitorUrl('');
      await loadData();
    } catch (error) {
      console.error('Error adding competitor:', error);
      alert('Failed to add competitor. Please try again.');
    }
  };

  const removeCompetitor = async (id: number) => {
    if (!confirm('Are you sure you want to remove this competitor?')) {
      return;
    }

    try {
      await api.deleteCompetitor(id);
      await loadData();
    } catch (error) {
      console.error('Error removing competitor:', error);
      alert('Failed to remove competitor. Please try again.');
    }
  };

  const getRadarData = () => {
    if (!myBusiness) return [];

    return [
      {
        metric: 'Avg Rating',
        myBusiness: ((myBusiness.avgRating || 0) / 5) * 100,
        ...competitors.slice(0, 3).reduce((acc, comp, idx) => ({
          ...acc,
          [`competitor${idx + 1}`]: ((comp.avgRating || 0) / 5) * 100
        }), {})
      },
      {
        metric: 'Response Rate',
        myBusiness: myBusiness.responseRate || 0,
        ...competitors.slice(0, 3).reduce((acc, comp, idx) => ({
          ...acc,
          [`competitor${idx + 1}`]: comp.responseRate || 0
        }), {})
      },
      {
        metric: 'Positive Sentiment',
        myBusiness: myBusiness.sentiment?.positive || 0,
        ...competitors.slice(0, 3).reduce((acc, comp, idx) => ({
          ...acc,
          [`competitor${idx + 1}`]: comp.sentiment?.positive || 0
        }), {})
      },
      {
        metric: 'Review Volume',
        myBusiness: Math.min(((myBusiness.totalReviews || 0) / 2000) * 100, 100),
        ...competitors.slice(0, 3).reduce((acc, comp, idx) => ({
          ...acc,
          [`competitor${idx + 1}`]: Math.min(((comp.totalReviews || 0) / 2000) * 100, 100)
        }), {})
      },
      {
        metric: 'Recent Growth',
        myBusiness: Math.min(((myBusiness.recentReviewCount || 0) / 200) * 100, 100),
        ...competitors.slice(0, 3).reduce((acc, comp, idx) => ({
          ...acc,
          [`competitor${idx + 1}`]: Math.min(((comp.recentReviewCount || 0) / 200) * 100, 100)
        }), {})
      }
    ];
  };

  const getComparisonTableData = () => {
    if (!myBusiness) return [];

    return [
      {
        name: myBusiness.name,
        avgRating: myBusiness.avgRating,
        totalReviews: myBusiness.totalReviews,
        responseRate: myBusiness.responseRate,
        avgResponseTime: myBusiness.avgResponseTime,
        sentiment: myBusiness.sentiment,
        ratingTrend: myBusiness.ratingTrend,
        isMyBusiness: true
      },
      ...competitors
    ];
  };

  const getMarketPosition = () => {
    if (!myBusiness) return { position: 0, total: 0 };

    const allBusinesses = [
      { name: myBusiness.name, avgRating: myBusiness.avgRating },
      ...competitors.map(c => ({ name: c.name, avgRating: c.avgRating }))
    ].sort((a, b) => b.avgRating - a.avgRating);

    const position = allBusinesses.findIndex(b => b.name === myBusiness.name) + 1;
    return { position, total: allBusinesses.length + 1 };
  };

  const getInsights = () => {
    if (!myBusiness) return [];

    const insights = [];
    const avgCompetitorRating = competitors.reduce((sum, c) => sum + c.avgRating, 0) / competitors.length;
    const avgCompetitorResponseRate = competitors.reduce((sum, c) => sum + c.responseRate, 0) / competitors.length;

    if (myBusiness.avgRating < avgCompetitorRating) {
      insights.push({
        type: 'warning',
        message: `Your rating (${myBusiness.avgRating.toFixed(1)}) is below the competitor average (${avgCompetitorRating.toFixed(1)})`
      });
    } else {
      insights.push({
        type: 'success',
        message: `Your rating (${myBusiness.avgRating.toFixed(1)}) is above the competitor average (${avgCompetitorRating.toFixed(1)})`
      });
    }

    if (myBusiness.responseRate < avgCompetitorResponseRate) {
      insights.push({
        type: 'warning',
        message: `Your response rate (${myBusiness.responseRate.toFixed(1)}%) is below the competitor average (${avgCompetitorResponseRate.toFixed(1)}%)`
      });
    }

    const bestCompetitor = competitors.reduce((best, c) => c.avgRating > best.avgRating ? c : best, competitors[0]);
    if (bestCompetitor && myBusiness.avgRating < bestCompetitor.avgRating) {
      insights.push({
        type: 'info',
        message: `${bestCompetitor.name} leads with ${bestCompetitor.avgRating.toFixed(1)} stars and ${bestCompetitor.responseRate.toFixed(1)}% response rate`
      });
    }

    return insights;
  };

  const filteredCompetitors = competitors.filter(c =>
    c.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const getTimeSince = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const minutes = Math.floor((now.getTime() - date.getTime()) / 60000);

    if (minutes < 60) return `${minutes}m ago`;
    if (minutes < 1440) return `${Math.floor(minutes / 60)}h ago`;
    return `${Math.floor(minutes / 1440)}d ago`;
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  const marketPosition = getMarketPosition();
  const insights = getInsights();

  return (
    <div className="px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-gray-900 flex items-center gap-3">
            <Target className="w-8 h-8 text-primary-600" />
            Competitor Analysis
          </h1>
          <p className="mt-2 text-gray-600">
            Compare your performance against competitors in real-time
          </p>
        </div>
        <div className="flex gap-3">
          <button
            onClick={loadData}
            className="btn-secondary flex items-center gap-2"
          >
            <RefreshCw className="w-4 h-4" />
            Refresh
          </button>
          <button
            onClick={() => setShowAddModal(true)}
            className="btn-primary flex items-center gap-2"
          >
            <Plus className="w-4 h-4" />
            Add Competitor
          </button>
        </div>
      </div>

      {/* Market Position & Quick Stats */}
      {myBusiness && competitors.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-4 gap-6 mb-6">
          {/* Market Position */}
          <div className="bg-white rounded-xl shadow-lg p-6 border border-gray-200">
            <div className="flex items-center gap-3 mb-3">
              <Trophy className="w-6 h-6 text-yellow-500" />
              <h3 className="font-semibold text-gray-900">Market Position</h3>
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-4xl font-bold text-gray-900">#{marketPosition.position}</span>
              <span className="text-gray-600">of {marketPosition.total}</span>
            </div>
            <p className="text-gray-600 text-sm mt-2">Based on average rating</p>
          </div>

          {/* Your Rating */}
          <div className="bg-white rounded-xl shadow-lg p-6 border border-gray-200">
            <div className="flex items-center gap-3 mb-3">
              <Star className="w-6 h-6 text-yellow-400 fill-current" />
              <h3 className="font-semibold text-gray-900">Your Rating</h3>
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-4xl font-bold text-gray-900">{myBusiness.avgRating.toFixed(1)}</span>
              <div className={`flex items-center gap-1 text-sm font-medium ${
                myBusiness.ratingTrend > 0 ? 'text-green-600' : 'text-red-600'
              }`}>
                {myBusiness.ratingTrend > 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                {Math.abs(myBusiness.ratingTrend)}%
              </div>
            </div>
            <p className="text-gray-600 text-sm mt-2">{myBusiness.totalReviews.toLocaleString()} total reviews</p>
          </div>

          {/* Response Rate */}
          <div className="bg-white rounded-xl shadow-lg p-6 border border-gray-200">
            <div className="flex items-center gap-3 mb-3">
              <MessageSquare className="w-6 h-6 text-blue-500" />
              <h3 className="font-semibold text-gray-900">Response Rate</h3>
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-4xl font-bold text-gray-900">{myBusiness.responseRate.toFixed(0)}%</span>
            </div>
            <p className="text-gray-600 text-sm mt-2">Avg {myBusiness.avgResponseTime.toFixed(1)}h response time</p>
          </div>

          {/* Sentiment */}
          <div className="bg-white rounded-xl shadow-lg p-6 border border-gray-200">
            <div className="flex items-center gap-3 mb-3">
              <ThumbsUp className="w-6 h-6 text-green-500" />
              <h3 className="font-semibold text-gray-900">Sentiment</h3>
            </div>
            <div className="flex items-baseline gap-2">
              <span className="text-4xl font-bold text-green-600">{myBusiness.sentiment.positive}%</span>
            </div>
            <p className="text-gray-600 text-sm mt-2">Positive feedback</p>
          </div>
        </div>
      )}

      {/* Insights */}
      {insights.length > 0 && (
        <div className="bg-white rounded-xl shadow-lg p-6 mb-6 border border-gray-200">
          <div className="flex items-center gap-2 mb-4">
            <Activity className="w-5 h-5 text-primary-600" />
            <h2 className="text-lg font-semibold text-gray-900">Key Insights</h2>
          </div>
          <div className="space-y-3">
            {insights.map((insight, idx) => (
              <div
                key={idx}
                className={`flex items-start gap-3 p-4 rounded-lg ${
                  insight.type === 'success' ? 'bg-green-50 border border-green-200' :
                  insight.type === 'warning' ? 'bg-yellow-50 border border-yellow-200' :
                  'bg-blue-50 border border-blue-200'
                }`}
              >
                {insight.type === 'success' && <Award className="w-5 h-5 text-green-600 flex-shrink-0 mt-0.5" />}
                {insight.type === 'warning' && <AlertCircle className="w-5 h-5 text-yellow-600 flex-shrink-0 mt-0.5" />}
                {insight.type === 'info' && <Crown className="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5" />}
                <p className={`text-sm ${
                  insight.type === 'success' ? 'text-green-800' :
                  insight.type === 'warning' ? 'text-yellow-800' :
                  'text-blue-800'
                }`}>
                  {insight.message}
                </p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* View Toggle */}
      {competitors.length > 0 && (
        <div className="bg-gray-100 rounded-lg shadow-sm p-1.5 mb-6 inline-flex gap-1">
          <button
            onClick={() => setSelectedView('overview')}
            className={`px-6 py-3 rounded-lg font-semibold transition-all ${
              selectedView === 'overview'
                ? 'bg-blue-600 text-white shadow-md'
                : 'bg-transparent text-gray-700 hover:bg-white'
            }`}
          >
            Overview Comparison
          </button>
          <button
            onClick={() => setSelectedView('detailed')}
            className={`px-6 py-3 rounded-lg font-semibold transition-all ${
              selectedView === 'detailed'
                ? 'bg-blue-600 text-white shadow-md'
                : 'bg-transparent text-gray-700 hover:bg-white'
            }`}
          >
            Detailed Analysis
          </button>
        </div>
      )}

      {/* Overview Comparison View */}
      {selectedView === 'overview' && myBusiness && competitors.length > 0 && (
        <>
          {/* Comparison Line Graph */}
          <div className="bg-white rounded-xl shadow-lg p-6 mb-6 border border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">Performance Metrics Graph</h2>
            <ResponsiveContainer width="100%" height={400}>
              <LineChart data={getRadarData()}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="metric" />
                <YAxis domain={[0, 100]} label={{ value: 'Performance (%)', angle: -90, position: 'insideLeft' }} />
                <Tooltip formatter={(value: any) => `${value.toFixed(1)}%`} />
                <Legend />
                <Line
                  name="Your Business"
                  dataKey="myBusiness"
                  stroke="#0284c7"
                  strokeWidth={3}
                  dot={{ fill: '#0284c7', r: 6 }}
                  activeDot={{ r: 8 }}
                />
                {competitors.slice(0, 3).map((comp, idx) => (
                  <Line
                    key={comp.id}
                    name={comp.name}
                    dataKey={`competitor${idx + 1}`}
                    stroke={['#f59e0b', '#ef4444', '#8b5cf6'][idx]}
                    strokeWidth={2}
                    dot={{ fill: ['#f59e0b', '#ef4444', '#8b5cf6'][idx], r: 5 }}
                    activeDot={{ r: 7 }}
                  />
                ))}
              </LineChart>
            </ResponsiveContainer>
          </div>

          {/* Comparison Table */}
          <div className="bg-white rounded-xl shadow-lg border border-gray-200 overflow-hidden mb-6">
            <div className="p-6 border-b border-gray-200">
              <h2 className="text-lg font-semibold text-gray-900">Head-to-Head Comparison</h2>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Business</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Rating</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Reviews</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Response Rate</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Avg Response Time</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Positive %</th>
                    <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Trend</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {getComparisonTableData().map((business: any, idx) => (
                    <tr key={idx} className={business.isMyBusiness ? 'bg-blue-50' : ''}>
                      <td className="px-6 py-4 whitespace-nowrap">
                        <div className="flex items-center gap-2">
                          {business.isMyBusiness && <Crown className="w-4 h-4 text-primary-600" />}
                          <span className={`text-sm font-medium ${business.isMyBusiness ? 'text-primary-600' : 'text-gray-900'}`}>
                            {business.name || 'Unknown'}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        <div className="flex items-center justify-center gap-1">
                          <Star className="w-4 h-4 text-yellow-400 fill-current" />
                          <span className="text-sm font-semibold text-gray-900">{(business.avgRating || 0).toFixed(1)}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center text-sm text-gray-900">
                        {(business.totalReviews || 0).toLocaleString()}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        <span className={`text-sm font-medium ${
                          (business.responseRate || 0) >= 90 ? 'text-green-600' :
                          (business.responseRate || 0) >= 70 ? 'text-yellow-600' :
                          'text-red-600'
                        }`}>
                          {(business.responseRate || 0).toFixed(1)}%
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        <span className={`text-sm font-medium ${
                          (business.avgResponseTime || 0) <= 4 ? 'text-green-600' :
                          (business.avgResponseTime || 0) <= 8 ? 'text-yellow-600' :
                          'text-red-600'
                        }`}>
                          {(business.avgResponseTime || 0).toFixed(1)}h
                        </span>
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center text-sm font-medium text-gray-900">
                        {business.sentiment?.positive || 0}%
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-center">
                        <div className={`flex items-center justify-center gap-1 text-sm font-medium ${
                          (business.ratingTrend || 0) > 0 ? 'text-green-600' : 'text-red-600'
                        }`}>
                          {(business.ratingTrend || 0) > 0 ? <TrendingUp className="w-4 h-4" /> : <TrendingDown className="w-4 h-4" />}
                          {Math.abs(business.ratingTrend || 0)}%
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}

      {/* Detailed Analysis View */}
      {selectedView === 'detailed' && (
        <>
          {/* Search */}
          <div className="bg-white rounded-lg shadow-md p-4 mb-6 border border-gray-200">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search competitors..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              />
            </div>
          </div>

          {/* Competitors Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {filteredCompetitors.map((competitor) => (
              <div
                key={competitor.id}
                className="bg-white rounded-xl shadow-lg p-6 border border-gray-200"
              >
                {/* Header */}
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <h3 className="text-lg font-semibold text-gray-900">
                        {competitor.name}
                      </h3>
                      <a
                        href={competitor.platformUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="text-primary-600 hover:text-primary-700"
                      >
                        <ExternalLink className="w-4 h-4" />
                      </a>
                    </div>
                    <div className="flex items-center gap-2 text-sm text-gray-600">
                      <span className="px-2 py-1 bg-gray-100 rounded text-xs font-medium">
                        {competitor.platform}
                      </span>
                      <span>Updated {getTimeSince(competitor.lastUpdated)}</span>
                    </div>
                  </div>
                  <button
                    onClick={() => removeCompetitor(competitor.id)}
                    className="p-2 text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                  >
                    <Trash2 className="w-5 h-5" />
                  </button>
                </div>

                {/* Key Metrics */}
                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div className="bg-gradient-to-br from-yellow-50 to-yellow-100 rounded-lg p-4 border border-yellow-200">
                    <div className="flex items-center justify-between mb-1">
                      <p className="text-sm text-gray-700 font-medium">Rating</p>
                      <div className={`flex items-center gap-1 text-xs font-medium ${
                        (competitor.ratingTrend || 0) > 0 ? 'text-green-600' : 'text-red-600'
                      }`}>
                        {(competitor.ratingTrend || 0) > 0 ? (
                          <TrendingUp className="w-3 h-3" />
                        ) : (
                          <TrendingDown className="w-3 h-3" />
                        )}
                        {Math.abs(competitor.ratingTrend || 0)}%
                      </div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Star className="w-5 h-5 text-yellow-500 fill-current" />
                      <span className="text-2xl font-bold text-gray-900">
                        {(competitor.avgRating || 0).toFixed(1)}
                      </span>
                    </div>
                  </div>

                  <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-lg p-4 border border-blue-200">
                    <p className="text-sm text-gray-700 font-medium mb-1">Reviews</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {(competitor.totalReviews || 0).toLocaleString()}
                    </p>
                    <p className="text-xs text-gray-600 mt-1">+{competitor.recentReviewCount || 0} recent</p>
                  </div>

                  <div className="bg-gradient-to-br from-green-50 to-green-100 rounded-lg p-4 border border-green-200">
                    <p className="text-sm text-gray-700 font-medium mb-1">Response Rate</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {(competitor.responseRate || 0).toFixed(1)}%
                    </p>
                  </div>

                  <div className="bg-gradient-to-br from-purple-50 to-purple-100 rounded-lg p-4 border border-purple-200">
                    <p className="text-sm text-gray-700 font-medium mb-1">Response Time</p>
                    <p className="text-2xl font-bold text-gray-900">
                      {(competitor.avgResponseTime || 0).toFixed(1)}h
                    </p>
                  </div>
                </div>

                {/* Sentiment */}
                <div className="mb-4">
                  <p className="text-sm font-medium text-gray-700 mb-3">Sentiment Breakdown</p>
                  <div className="space-y-2">
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-medium text-gray-600 w-16">Positive</span>
                      <div className="flex-1 bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-green-500 h-2 rounded-full"
                          style={{ width: `${competitor.sentiment?.positive || 0}%` }}
                        ></div>
                      </div>
                      <span className="text-xs font-medium text-gray-900 w-10 text-right">
                        {competitor.sentiment?.positive || 0}%
                      </span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-medium text-gray-600 w-16">Neutral</span>
                      <div className="flex-1 bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-yellow-500 h-2 rounded-full"
                          style={{ width: `${competitor.sentiment?.neutral || 0}%` }}
                        ></div>
                      </div>
                      <span className="text-xs font-medium text-gray-900 w-10 text-right">
                        {competitor.sentiment?.neutral || 0}%
                      </span>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className="text-xs font-medium text-gray-600 w-16">Negative</span>
                      <div className="flex-1 bg-gray-200 rounded-full h-2">
                        <div
                          className="bg-red-500 h-2 rounded-full"
                          style={{ width: `${competitor.sentiment?.negative || 0}%` }}
                        ></div>
                      </div>
                      <span className="text-xs font-medium text-gray-900 w-10 text-right">
                        {competitor.sentiment?.negative || 0}%
                      </span>
                    </div>
                  </div>
                </div>

                {/* Rating Distribution */}
                <div>
                  <p className="text-sm font-medium text-gray-700 mb-3">Rating Distribution</p>
                  <div className="space-y-2">
                    {competitor.reviewDistribution.map((item) => {
                      const percentage = (item.count / competitor.totalReviews) * 100;
                      return (
                        <div key={item.rating} className="flex items-center gap-3">
                          <div className="flex items-center gap-1 w-12">
                            <span className="text-sm text-gray-700 font-medium">{item.rating}</span>
                            <Star className="w-3 h-3 text-yellow-400 fill-current" />
                          </div>
                          <div className="flex-1 bg-gray-200 rounded-full h-2">
                            <div
                              className="bg-primary-600 h-2 rounded-full transition-all duration-300"
                              style={{ width: `${percentage}%` }}
                            ></div>
                          </div>
                          <span className="text-xs text-gray-600 w-12 text-right font-medium">
                            {item.count}
                          </span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Empty State */}
          {filteredCompetitors.length === 0 && (
            <div className="bg-white rounded-lg shadow-md p-12 text-center border border-gray-200">
              <Users className="w-16 h-16 text-gray-400 mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                {searchQuery ? 'No competitors found' : 'No competitors added yet'}
              </h3>
              <p className="text-gray-600 mb-6">
                {searchQuery
                  ? 'Try adjusting your search query'
                  : 'Start tracking your competitors to compare performance'}
              </p>
              {!searchQuery && (
                <button
                  onClick={() => setShowAddModal(true)}
                  className="btn-primary inline-flex items-center gap-2"
                >
                  <Plus className="w-4 h-4" />
                  Add Your First Competitor
                </button>
              )}
            </div>
          )}
        </>
      )}

      {/* Add Competitor Modal */}
      {showAddModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-xl shadow-2xl max-w-md w-full p-6">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Add Competitor</h2>

            <div className="space-y-4 mb-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Business Name
                </label>
                <input
                  type="text"
                  value={newCompetitorName}
                  onChange={(e) => setNewCompetitorName(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="e.g., Main Street Cafe"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Platform
                </label>
                <select
                  value={newCompetitorPlatform}
                  onChange={(e) => setNewCompetitorPlatform(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                >
                  <option value="Google">Google</option>
                  <option value="Yelp">Yelp</option>
                  <option value="Facebook">Facebook</option>
                  <option value="TripAdvisor">TripAdvisor</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Profile URL
                </label>
                <input
                  type="url"
                  value={newCompetitorUrl}
                  onChange={(e) => setNewCompetitorUrl(e.target.value)}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                  placeholder="https://..."
                />
              </div>
            </div>

            <div className="flex gap-3 justify-end">
              <button
                onClick={() => {
                  setShowAddModal(false);
                  setNewCompetitorName('');
                  setNewCompetitorPlatform('Google');
                  setNewCompetitorUrl('');
                }}
                className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 font-medium"
              >
                Cancel
              </button>
              <button
                onClick={addCompetitor}
                className="btn-primary"
              >
                Add Competitor
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
