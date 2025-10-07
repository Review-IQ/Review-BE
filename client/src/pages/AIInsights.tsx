import { useState, useEffect } from 'react';
import { Sparkles, TrendingUp, Users as UsersIcon, Lightbulb, FileText, RefreshCw, BarChart2, Target, Star, ChevronDown, ChevronUp } from 'lucide-react';
import { api } from '../services/api';
import { useLocation } from '../contexts/LocationContext';

export function AIInsights() {
  const { selectedLocationId } = useLocation();
  const [businessId] = useState(1); // TODO: Get from context/state
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [selectedPeriod, setSelectedPeriod] = useState(30);

  const [analyticsInsights, setAnalyticsInsights] = useState<string>('');
  const [competitorInsights, setCompetitorInsights] = useState<string>('');
  const [reviewSummary, setReviewSummary] = useState<{ summary: string; reviewCount: number; period: string } | null>(null);
  const [recommendations, setRecommendations] = useState<string[]>([]);

  // Collapse states
  const [reviewSummaryCollapsed, setReviewSummaryCollapsed] = useState(false);
  const [analyticsCollapsed, setAnalyticsCollapsed] = useState(false);
  const [competitorCollapsed, setCompetitorCollapsed] = useState(false);
  const [recommendationsCollapsed, setRecommendationsCollapsed] = useState(false);

  useEffect(() => {
    loadInsights();
  }, [selectedPeriod, selectedLocationId]);

  const loadInsights = async () => {
    try {
      setLoading(true);

      // Load all AI insights in parallel
      const [analytics, competitors, summary, recs] = await Promise.all([
        api.getAnalyticsInsights(businessId, selectedLocationId),
        api.getCompetitorInsights(businessId, selectedLocationId),
        api.getReviewSummary(businessId, selectedPeriod, selectedLocationId),
        api.getActionableRecommendations(businessId, selectedLocationId)
      ]);

      setAnalyticsInsights(analytics.data.insights);
      setCompetitorInsights(competitors.data.insights);
      setReviewSummary(summary.data);
      setRecommendations(recs.data.recommendations);
    } catch (error) {
      console.error('Error loading AI insights:', error);
    } finally {
      setLoading(false);
    }
  };

  const refreshInsights = async () => {
    setRefreshing(true);
    await loadInsights();
    setRefreshing(false);
  };

  if (loading) {
    return (
      <div className="flex justify-center items-center h-screen bg-gradient-to-br from-purple-50 via-blue-50 to-pink-50">
        <div className="text-center">
          <div className="relative">
            <div className="animate-spin rounded-full h-16 w-16 border-4 border-purple-200 border-t-purple-600 mx-auto mb-4"></div>
            <Sparkles className="w-6 h-6 text-purple-600 absolute top-1/2 left-1/2 transform -translate-x-1/2 -translate-y-1/2" />
          </div>
          <p className="text-lg font-medium text-gray-700">Generating AI insights...</p>
          <p className="text-sm text-gray-500 mt-1">Analyzing your business data</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-purple-50 via-blue-50 to-pink-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <div className="flex items-center gap-3 mb-2">
                <div className="w-12 h-12 bg-gradient-to-br from-purple-600 via-blue-600 to-pink-600 rounded-xl flex items-center justify-center shadow-lg">
                  <Sparkles className="w-7 h-7 text-white" />
                </div>
                <h1 className="text-3xl sm:text-4xl font-bold bg-gradient-to-r from-purple-600 via-blue-600 to-pink-600 bg-clip-text text-transparent">
                  AI Insights
                </h1>
              </div>
              <p className="text-gray-600 ml-15">
                AI-powered analysis and recommendations for your business
              </p>
            </div>
            <div className="flex items-center gap-3">
              <select
                value={selectedPeriod}
                onChange={(e) => setSelectedPeriod(Number(e.target.value))}
                className="px-4 py-2.5 border border-gray-300 rounded-lg bg-white shadow-sm focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all"
              >
                <option value={7}>Last 7 days</option>
                <option value={30}>Last 30 days</option>
                <option value={90}>Last 90 days</option>
              </select>
              <button
                onClick={refreshInsights}
                disabled={refreshing}
                className="px-5 py-2.5 bg-gradient-to-r from-purple-600 to-blue-600 text-white font-medium rounded-lg hover:from-purple-700 hover:to-blue-700 transition-all shadow-lg hover:shadow-xl disabled:opacity-50 flex items-center gap-2"
              >
                <RefreshCw className={`w-4 h-4 ${refreshing ? 'animate-spin' : ''}`} />
                {refreshing ? 'Refreshing...' : 'Refresh'}
              </button>
            </div>
          </div>
        </div>

        {/* Stats Overview */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8">
          <div className="bg-white rounded-xl shadow-md p-6 border border-gray-100 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Reviews Analyzed</p>
                <p className="text-3xl font-bold text-gray-900 mt-1">{reviewSummary?.reviewCount || 0}</p>
              </div>
              <div className="w-12 h-12 bg-blue-100 rounded-xl flex items-center justify-center">
                <FileText className="w-6 h-6 text-blue-600" />
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-md p-6 border border-gray-100 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Time Period</p>
                <p className="text-3xl font-bold text-gray-900 mt-1">{selectedPeriod}d</p>
              </div>
              <div className="w-12 h-12 bg-purple-100 rounded-xl flex items-center justify-center">
                <BarChart2 className="w-6 h-6 text-purple-600" />
              </div>
            </div>
          </div>
          <div className="bg-white rounded-xl shadow-md p-6 border border-gray-100 hover:shadow-lg transition-shadow">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Recommendations</p>
                <p className="text-3xl font-bold text-gray-900 mt-1">{recommendations.length}</p>
              </div>
              <div className="w-12 h-12 bg-yellow-100 rounded-xl flex items-center justify-center">
                <Target className="w-6 h-6 text-yellow-600" />
              </div>
            </div>
          </div>
        </div>

        {/* AI Insights Sections */}
        <div className="space-y-6">
          {/* Review Summary */}
          <div className="group relative bg-gradient-to-br from-blue-500 to-blue-600 rounded-2xl shadow-xl p-1 hover:shadow-2xl transition-all">
            <div className="bg-white rounded-[14px] p-6">
              <div
                className="flex items-center gap-3 cursor-pointer"
                onClick={() => setReviewSummaryCollapsed(!reviewSummaryCollapsed)}
              >
                <div className="w-12 h-12 bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl flex items-center justify-center shadow-lg">
                  <FileText className="w-6 h-6 text-white" />
                </div>
                <div className="flex-1">
                  <h2 className="text-2xl font-bold text-gray-900 group-hover:text-blue-600 transition-colors">Review Summary</h2>
                  {reviewSummary && (
                    <p className="text-sm font-medium text-gray-500 mt-1">
                      <span className="inline-flex items-center gap-1">
                        <span className="w-2 h-2 bg-blue-500 rounded-full animate-pulse"></span>
                        {reviewSummary.reviewCount} reviews • {reviewSummary.period}
                      </span>
                    </p>
                  )}
                </div>
                <button className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
                  {reviewSummaryCollapsed ? <ChevronDown className="w-6 h-6 text-gray-600" /> : <ChevronUp className="w-6 h-6 text-gray-600" />}
                </button>
              </div>
              {!reviewSummaryCollapsed && (
                <div className="relative overflow-hidden bg-gradient-to-br from-blue-50 via-blue-100 to-blue-50 rounded-xl p-6 shadow-inner mt-5">
                  <div className="absolute top-0 right-0 w-32 h-32 bg-blue-200 rounded-full opacity-20 -mr-16 -mt-16"></div>
                  <div className="absolute bottom-0 left-0 w-24 h-24 bg-blue-300 rounded-full opacity-20 -ml-12 -mb-12"></div>
                  <p className="relative text-gray-800 leading-relaxed whitespace-pre-line text-base">{reviewSummary?.summary || 'No reviews available for analysis.'}</p>
                </div>
              )}
            </div>
          </div>

          {/* Analytics Insights */}
          <div className="group relative bg-gradient-to-br from-green-500 to-emerald-600 rounded-2xl shadow-xl p-1 hover:shadow-2xl transition-all">
            <div className="bg-white rounded-[14px] p-6">
              <div
                className="flex items-center gap-3 cursor-pointer"
                onClick={() => setAnalyticsCollapsed(!analyticsCollapsed)}
              >
                <div className="w-12 h-12 bg-gradient-to-br from-green-500 to-emerald-600 rounded-xl flex items-center justify-center shadow-lg">
                  <TrendingUp className="w-6 h-6 text-white" />
                </div>
                <div className="flex-1">
                  <h2 className="text-2xl font-bold text-gray-900 group-hover:text-green-600 transition-colors">Analytics Insights</h2>
                  <p className="text-sm font-medium text-gray-500 mt-1">
                    <span className="inline-flex items-center gap-1">
                      <span className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></span>
                      Performance analysis
                    </span>
                  </p>
                </div>
                <button className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
                  {analyticsCollapsed ? <ChevronDown className="w-6 h-6 text-gray-600" /> : <ChevronUp className="w-6 h-6 text-gray-600" />}
                </button>
              </div>
              {!analyticsCollapsed && (
                <div className="relative overflow-hidden bg-gradient-to-br from-green-50 via-emerald-100 to-green-50 rounded-xl p-6 shadow-inner mt-5">
                  <div className="absolute top-0 right-0 w-32 h-32 bg-green-200 rounded-full opacity-20 -mr-16 -mt-16"></div>
                  <div className="absolute bottom-0 left-0 w-24 h-24 bg-emerald-300 rounded-full opacity-20 -ml-12 -mb-12"></div>
                  <p className="relative text-gray-800 leading-relaxed whitespace-pre-line text-base">{analyticsInsights}</p>
                </div>
              )}
            </div>
          </div>

          {/* Competitor Insights */}
          <div className="group relative bg-gradient-to-r from-purple-500 via-purple-600 to-pink-600 rounded-2xl shadow-xl p-1 hover:shadow-2xl transition-all">
            <div className="bg-white rounded-[14px] p-6">
              <div
                className="flex items-center gap-4 cursor-pointer"
                onClick={() => setCompetitorCollapsed(!competitorCollapsed)}
              >
                <div className="w-14 h-14 bg-gradient-to-br from-purple-500 via-purple-600 to-pink-600 rounded-xl flex items-center justify-center shadow-lg">
                  <UsersIcon className="w-7 h-7 text-white" />
                </div>
                <div className="flex-1">
                  <h2 className="text-2xl font-bold text-gray-900 group-hover:text-purple-600 transition-colors">Competitive Analysis</h2>
                  <p className="text-sm font-medium text-gray-500 mt-1">
                    <span className="inline-flex items-center gap-1">
                      <span className="w-2 h-2 bg-purple-500 rounded-full animate-pulse"></span>
                      Your position in the market
                    </span>
                  </p>
                </div>
                <button className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
                  {competitorCollapsed ? <ChevronDown className="w-6 h-6 text-gray-600" /> : <ChevronUp className="w-6 h-6 text-gray-600" />}
                </button>
              </div>
              {!competitorCollapsed && (
                <div className="relative overflow-hidden bg-gradient-to-br from-purple-50 via-pink-50 to-purple-50 rounded-xl p-7 shadow-inner mt-6">
                  <div className="absolute top-0 right-0 w-40 h-40 bg-purple-200 rounded-full opacity-20 -mr-20 -mt-20"></div>
                  <div className="absolute bottom-0 left-0 w-32 h-32 bg-pink-200 rounded-full opacity-20 -ml-16 -mb-16"></div>
                  <div className="absolute top-1/2 left-1/2 w-24 h-24 bg-purple-300 rounded-full opacity-10 transform -translate-x-1/2 -translate-y-1/2"></div>
                  <p className="relative text-gray-800 leading-relaxed whitespace-pre-line text-base">{competitorInsights}</p>
                </div>
              )}
            </div>
          </div>

          {/* Actionable Recommendations */}
          <div className="group relative bg-gradient-to-r from-yellow-500 via-orange-500 to-yellow-600 rounded-2xl shadow-xl p-1 hover:shadow-2xl transition-all">
            <div className="bg-white rounded-[14px] p-6">
              <div
                className="flex items-center gap-3 cursor-pointer"
                onClick={() => setRecommendationsCollapsed(!recommendationsCollapsed)}
              >
                <div className="w-12 h-12 bg-gradient-to-br from-yellow-500 to-orange-600 rounded-xl flex items-center justify-center shadow-lg">
                  <Lightbulb className="w-6 h-6 text-white" />
                </div>
                <div className="flex-1">
                  <h2 className="text-2xl font-bold text-gray-900 group-hover:text-yellow-600 transition-colors">Actionable Recommendations</h2>
                  <p className="text-sm font-medium text-gray-500 mt-1">
                    <span className="inline-flex items-center gap-1">
                      <span className="w-2 h-2 bg-yellow-500 rounded-full animate-pulse"></span>
                      {recommendations.length} data-driven suggestions
                    </span>
                  </p>
                </div>
                <button className="p-2 hover:bg-gray-100 rounded-lg transition-colors">
                  {recommendationsCollapsed ? <ChevronDown className="w-6 h-6 text-gray-600" /> : <ChevronUp className="w-6 h-6 text-gray-600" />}
                </button>
              </div>
              {!recommendationsCollapsed && (
                <div className="space-y-3 mt-5">
                  {recommendations.length > 0 ? (
                    recommendations.map((rec, index) => (
                      <div
                        key={index}
                        className="group flex items-start gap-4 p-5 bg-gradient-to-r from-yellow-50 to-orange-50 border border-yellow-200 rounded-xl hover:shadow-md transition-all hover:border-yellow-300"
                      >
                        <div className="flex-shrink-0 w-8 h-8 bg-gradient-to-br from-yellow-500 to-orange-500 text-white rounded-lg flex items-center justify-center text-sm font-bold shadow-md group-hover:scale-110 transition-transform">
                          {index + 1}
                        </div>
                        <p className="text-gray-800 flex-1 leading-relaxed pt-0.5">{rec}</p>
                      </div>
                    ))
                  ) : (
                    <div className="p-6 bg-gray-50 border border-gray-200 rounded-xl text-center">
                      <Star className="w-12 h-12 text-gray-400 mx-auto mb-3" />
                      <p className="text-gray-600 font-medium">No recommendations available</p>
                      <p className="text-gray-500 text-sm mt-1">Gather more reviews to get AI-powered insights</p>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Info Footer */}
        <div className="mt-8 bg-gradient-to-r from-purple-100 via-blue-100 to-pink-100 border border-purple-200 rounded-xl p-6 shadow-md">
          <div className="flex items-start gap-4">
            <div className="w-10 h-10 bg-gradient-to-br from-purple-600 to-pink-600 rounded-lg flex items-center justify-center flex-shrink-0 shadow-md">
              <Sparkles className="w-5 h-5 text-white" />
            </div>
            <div>
              <h3 className="font-bold text-gray-900 mb-2 text-lg">Powered by OpenAI GPT-4</h3>
              <p className="text-sm text-gray-700 leading-relaxed">
                These insights are generated using advanced AI to analyze your reviews, analytics, and competitive data.
                The recommendations are tailored specifically to your business based on real customer feedback and market trends.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
