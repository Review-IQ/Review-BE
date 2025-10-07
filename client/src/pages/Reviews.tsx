import { useState, useEffect } from 'react';
import {
  Star,
  Filter,
  Search,
  ChevronLeft,
  ChevronRight,
  MessageSquare,
  Flag,
  Check,
  Send,
  Sparkles,
  RefreshCw
} from 'lucide-react';
import { api } from '../services/api';
import { useLocation } from '../contexts/LocationContext';

interface Review {
  id: number;
  businessId: number;
  businessName: string;
  platform: string;
  platformReviewId: string;
  reviewerName: string;
  reviewerEmail?: string;
  reviewerAvatarUrl?: string;
  rating: number;
  reviewText: string;
  reviewDate: string;
  responseText?: string;
  responseDate?: string;
  sentiment?: string;
  sentimentScore?: number;
  aiSuggestedResponse?: string;
  isRead: boolean;
  isFlagged: boolean;
  location?: string;
}

interface PaginationInfo {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function Reviews() {
  const { selectedLocationId } = useLocation();
  const [reviews, setReviews] = useState<Review[]>([]);
  const [pagination, setPagination] = useState<PaginationInfo>({
    page: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0
  });
  const [loading, setLoading] = useState(true);
  const [selectedReview, setSelectedReview] = useState<Review | null>(null);
  const [replyText, setReplyText] = useState('');
  const [showReplyModal, setShowReplyModal] = useState(false);
  const [generatingAI, setGeneratingAI] = useState(false);
  const [improvingAI, setImprovingAI] = useState(false);

  // Filters
  const [selectedPlatform, setSelectedPlatform] = useState<string>('all');
  const [selectedSentiment, setSelectedSentiment] = useState<string>('all');
  const [selectedRating, setSelectedRating] = useState<string>('all');
  const [showUnreadOnly, setShowUnreadOnly] = useState(false);
  const [showFlaggedOnly, setShowFlaggedOnly] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  useEffect(() => {
    loadReviews();
  }, [pagination.page, selectedPlatform, selectedSentiment, selectedRating, showUnreadOnly, showFlaggedOnly, selectedLocationId]);

  const loadReviews = async () => {
    try {
      setLoading(true);

      const params: any = {
        page: pagination.page,
        pageSize: pagination.pageSize
      };

      if (selectedPlatform !== 'all') params.platform = selectedPlatform;
      if (selectedSentiment !== 'all') params.sentiment = selectedSentiment;
      if (selectedRating !== 'all') params.rating = parseInt(selectedRating);
      if (showUnreadOnly) params.isRead = false;
      if (showFlaggedOnly) params.isFlagged = true;
      if (selectedLocationId) params.locationId = selectedLocationId;

      const response = await api.getReviews(params);
      const data = response.data;

      setReviews(data.reviews || []);
      setPagination({
        page: data.page || 1,
        pageSize: data.pageSize || 10,
        totalCount: data.totalCount || 0,
        totalPages: data.totalPages || 0
      });

      // Fallback to mock data if no reviews
      if (!data.reviews || data.reviews.length === 0) {
        const mockReviews: Review[] = [
        {
          id: 1,
          businessId: 1,
          businessName: 'Main Street Cafe',
          platform: 'Google',
          platformReviewId: 'google-123',
          reviewerName: 'John Doe',
          reviewerEmail: 'john@example.com',
          rating: 5,
          reviewText: 'Amazing food and great service! The atmosphere was perfect for a date night. Will definitely come back!',
          reviewDate: new Date().toISOString(),
          sentiment: 'Positive',
          sentimentScore: 0.95,
          aiSuggestedResponse: 'Thank you so much for your wonderful review, John! We\'re thrilled you enjoyed your date night with us. We look forward to welcoming you back soon!',
          isRead: false,
          isFlagged: false
        },
        {
          id: 2,
          businessId: 1,
          businessName: 'Main Street Cafe',
          platform: 'Yelp',
          platformReviewId: 'yelp-456',
          reviewerName: 'Jane Smith',
          rating: 4,
          reviewText: 'Good experience overall. The food was delicious but service was a bit slow.',
          reviewDate: new Date(Date.now() - 86400000).toISOString(),
          sentiment: 'Positive',
          sentimentScore: 0.65,
          responseText: 'Thank you for your feedback! We apologize for the slow service and are working to improve.',
          responseDate: new Date(Date.now() - 43200000).toISOString(),
          isRead: true,
          isFlagged: false
        },
        {
          id: 3,
          businessId: 1,
          businessName: 'Main Street Cafe',
          platform: 'Facebook',
          platformReviewId: 'fb-789',
          reviewerName: 'Mike Johnson',
          rating: 2,
          reviewText: 'Disappointed with the quality. Food was cold and staff seemed uninterested.',
          reviewDate: new Date(Date.now() - 172800000).toISOString(),
          sentiment: 'Negative',
          sentimentScore: -0.75,
          aiSuggestedResponse: 'We sincerely apologize for your experience, Mike. This is not the standard we aim for. Please reach out to us directly so we can make this right.',
          isRead: true,
          isFlagged: true
        }
      ];

        setReviews(mockReviews);
        setPagination({
          page: 1,
          pageSize: 10,
          totalCount: 47,
          totalPages: 5
        });
      }
    } catch (error) {
      console.error('Error loading reviews:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleReply = async (review: Review) => {
    setSelectedReview(review);
    setReplyText(review.aiSuggestedResponse || '');
    setShowReplyModal(true);
  };

  const submitReply = async () => {
    if (!selectedReview || !replyText.trim()) return;

    try {
      await api.replyToReview(selectedReview.id, replyText);
      setShowReplyModal(false);
      setReplyText('');
      setSelectedReview(null);
      await loadReviews();
    } catch (error) {
      console.error('Error submitting reply:', error);
    }
  };

  const toggleReadStatus = async (review: Review) => {
    try {
      await api.markReviewAsRead(review.id, !review.isRead);
      await loadReviews();
    } catch (error) {
      console.error('Error toggling read status:', error);
    }
  };

  const toggleFlagStatus = async (review: Review) => {
    try {
      await api.flagReview(review.id, !review.isFlagged);
      await loadReviews();
    } catch (error) {
      console.error('Error toggling flag status:', error);
    }
  };

  const generateAIResponse = async () => {
    if (!selectedReview) return;

    try {
      setGeneratingAI(true);
      const response = await api.generateReviewResponse(selectedReview.id);
      setReplyText(response.data.response);
    } catch (error) {
      console.error('Error generating AI response:', error);
      alert('Failed to generate AI response. Please try again.');
    } finally {
      setGeneratingAI(false);
    }
  };

  const improveAIResponse = async () => {
    if (!selectedReview || !replyText.trim()) return;

    try {
      setImprovingAI(true);
      const response = await api.improveReviewResponse(selectedReview.id, replyText);
      setReplyText(response.data.response);
    } catch (error) {
      console.error('Error improving response:', error);
      alert('Failed to improve response. Please try again.');
    } finally {
      setImprovingAI(false);
    }
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

  const getSentimentColor = (sentiment?: string) => {
    switch (sentiment?.toLowerCase()) {
      case 'positive':
        return 'bg-green-100 text-green-800';
      case 'negative':
        return 'bg-red-100 text-red-800';
      case 'neutral':
        return 'bg-gray-100 text-gray-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const getTimeSince = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (seconds < 60) return 'Just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    return date.toLocaleDateString();
  };

  const filteredReviews = reviews.filter(review => {
    if (searchQuery && !review.reviewText.toLowerCase().includes(searchQuery.toLowerCase()) &&
        !review.reviewerName.toLowerCase().includes(searchQuery.toLowerCase())) {
      return false;
    }
    return true;
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-gray-900">All Reviews</h1>
        <p className="mt-2 text-gray-600">
          Manage reviews from all your connected platforms
        </p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow-md p-6 mb-6 border border-gray-200">
        <div className="flex items-center gap-2 mb-4">
          <Filter className="w-5 h-5 text-gray-600" />
          <h2 className="text-lg font-semibold text-gray-900">Filters</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          {/* Search */}
          <div className="lg:col-span-2">
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Search Reviews
            </label>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search by reviewer or content..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
              />
            </div>
          </div>

          {/* Platform Filter */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Platform
            </label>
            <select
              value={selectedPlatform}
              onChange={(e) => setSelectedPlatform(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            >
              <option value="all">All Platforms</option>
              <option value="Google">Google</option>
              <option value="Yelp">Yelp</option>
              <option value="Facebook">Facebook</option>
              <option value="TripAdvisor">TripAdvisor</option>
            </select>
          </div>

          {/* Sentiment Filter */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Sentiment
            </label>
            <select
              value={selectedSentiment}
              onChange={(e) => setSelectedSentiment(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            >
              <option value="all">All Sentiments</option>
              <option value="Positive">Positive</option>
              <option value="Neutral">Neutral</option>
              <option value="Negative">Negative</option>
            </select>
          </div>

          {/* Rating Filter */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Rating
            </label>
            <select
              value={selectedRating}
              onChange={(e) => setSelectedRating(e.target.value)}
              className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
            >
              <option value="all">All Ratings</option>
              <option value="5">5 Stars</option>
              <option value="4">4 Stars</option>
              <option value="3">3 Stars</option>
              <option value="2">2 Stars</option>
              <option value="1">1 Star</option>
            </select>
          </div>
        </div>

        {/* Quick Filters */}
        <div className="flex gap-4 mt-4">
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={showUnreadOnly}
              onChange={(e) => setShowUnreadOnly(e.target.checked)}
              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
            />
            <span className="text-sm text-gray-700">Unread only</span>
          </label>
          <label className="flex items-center gap-2 cursor-pointer">
            <input
              type="checkbox"
              checked={showFlaggedOnly}
              onChange={(e) => setShowFlaggedOnly(e.target.checked)}
              className="w-4 h-4 text-primary-600 border-gray-300 rounded focus:ring-primary-500"
            />
            <span className="text-sm text-gray-700">Flagged only</span>
          </label>
        </div>
      </div>

      {/* Reviews List */}
      {loading ? (
        <div className="flex justify-center items-center h-64">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
        </div>
      ) : (
        <>
          <div className="space-y-4 mb-6">
            {filteredReviews.map((review) => (
              <div
                key={review.id}
                className={`bg-white rounded-lg shadow-md p-6 border ${
                  review.isRead ? 'border-gray-200' : 'border-primary-300 bg-primary-50'
                }`}
              >
                {/* Review Header */}
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <div className="w-10 h-10 bg-gray-300 rounded-full flex items-center justify-center">
                        <span className="text-lg font-semibold text-gray-600">
                          {review.reviewerName.charAt(0).toUpperCase()}
                        </span>
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">{review.reviewerName}</h3>
                        <div className="flex items-center gap-2 text-sm text-gray-500">
                          <span>{review.businessName}</span>
                          <span>•</span>
                          <span>{getTimeSince(review.reviewDate)}</span>
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-3 mb-3">
                      {renderStars(review.rating)}
                      <span className="px-2 py-1 bg-gray-100 text-gray-700 text-xs rounded">
                        {review.platform}
                      </span>
                      {review.sentiment && (
                        <span className={`px-2 py-1 text-xs rounded ${getSentimentColor(review.sentiment)}`}>
                          {review.sentiment}
                        </span>
                      )}
                      {!review.isRead && (
                        <span className="px-2 py-1 bg-primary-600 text-white text-xs rounded">
                          New
                        </span>
                      )}
                      {review.isFlagged && (
                        <Flag className="w-4 h-4 text-red-600 fill-current" />
                      )}
                    </div>
                  </div>

                  {/* Action Buttons */}
                  <div className="flex gap-2">
                    <button
                      onClick={() => toggleReadStatus(review)}
                      className={`p-2 rounded-lg transition-colors ${
                        review.isRead
                          ? 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                          : 'bg-primary-100 text-primary-600 hover:bg-primary-200'
                      }`}
                      title={review.isRead ? 'Mark as unread' : 'Mark as read'}
                    >
                      <Check className="w-5 h-5" />
                    </button>
                    <button
                      onClick={() => toggleFlagStatus(review)}
                      className={`p-2 rounded-lg transition-colors ${
                        review.isFlagged
                          ? 'bg-red-100 text-red-600 hover:bg-red-200'
                          : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                      }`}
                      title={review.isFlagged ? 'Unflag' : 'Flag'}
                    >
                      <Flag className={`w-5 h-5 ${review.isFlagged ? 'fill-current' : ''}`} />
                    </button>
                  </div>
                </div>

                {/* Review Text */}
                <p className="text-gray-700 mb-4">{review.reviewText}</p>

                {/* Response Section */}
                {review.responseText ? (
                  <div className="bg-blue-50 border border-blue-100 rounded-lg p-4 mb-3">
                    <div className="flex items-center gap-2 mb-2">
                      <MessageSquare className="w-4 h-4 text-blue-600" />
                      <span className="text-sm font-medium text-blue-900">Your Response</span>
                      <span className="text-xs text-blue-600">
                        {getTimeSince(review.responseDate!)}
                      </span>
                    </div>
                    <p className="text-sm text-blue-900">{review.responseText}</p>
                  </div>
                ) : (
                  <div className="flex gap-3">
                    <button
                      onClick={() => handleReply(review)}
                      className="btn-primary flex items-center gap-2"
                    >
                      <MessageSquare className="w-4 h-4" />
                      Reply to Review
                    </button>
                    {review.aiSuggestedResponse && (
                      <button
                        onClick={() => handleReply(review)}
                        className="px-4 py-2 bg-purple-100 text-purple-700 rounded-lg hover:bg-purple-200 transition-colors flex items-center gap-2"
                      >
                        <span className="text-sm">✨ AI Suggested Response</span>
                      </button>
                    )}
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* Pagination */}
          <div className="bg-white rounded-lg shadow-md p-4 border border-gray-200">
            <div className="flex items-center justify-between">
              <div className="text-sm text-gray-600">
                Showing {((pagination.page - 1) * pagination.pageSize) + 1} to{' '}
                {Math.min(pagination.page * pagination.pageSize, pagination.totalCount)} of{' '}
                {pagination.totalCount} reviews
              </div>
              <div className="flex gap-2">
                <button
                  onClick={() => setPagination({ ...pagination, page: pagination.page - 1 })}
                  disabled={pagination.page === 1}
                  className="p-2 rounded-lg border border-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  <ChevronLeft className="w-5 h-5" />
                </button>
                <div className="flex gap-1">
                  {Array.from({ length: pagination.totalPages }, (_, i) => i + 1).map((page) => (
                    <button
                      key={page}
                      onClick={() => setPagination({ ...pagination, page })}
                      className={`px-3 py-2 rounded-lg ${
                        page === pagination.page
                          ? 'bg-primary-600 text-white'
                          : 'bg-white border border-gray-300 text-gray-700 hover:bg-gray-50'
                      }`}
                    >
                      {page}
                    </button>
                  ))}
                </div>
                <button
                  onClick={() => setPagination({ ...pagination, page: pagination.page + 1 })}
                  disabled={pagination.page === pagination.totalPages}
                  className="p-2 rounded-lg border border-gray-300 disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
                >
                  <ChevronRight className="w-5 h-5" />
                </button>
              </div>
            </div>
          </div>
        </>
      )}

      {/* Reply Modal */}
      {showReplyModal && selectedReview && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full p-6">
            <h2 className="text-2xl font-bold text-gray-900 mb-4">Reply to Review</h2>

            {/* Review Preview */}
            <div className="bg-gray-50 rounded-lg p-4 mb-4">
              <div className="flex items-center gap-2 mb-2">
                <span className="font-medium text-gray-900">{selectedReview.reviewerName}</span>
                {renderStars(selectedReview.rating)}
              </div>
              <p className="text-sm text-gray-700">{selectedReview.reviewText}</p>
            </div>

            {/* AI Quick Actions */}
            <div className="mb-4 flex gap-2">
              <button
                onClick={generateAIResponse}
                disabled={generatingAI}
                className="flex-1 px-4 py-2 bg-gradient-to-r from-purple-600 to-blue-600 text-white rounded-lg hover:from-purple-700 hover:to-blue-700 transition-colors flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Sparkles className={`w-4 h-4 ${generatingAI ? 'animate-spin' : ''}`} />
                {generatingAI ? 'Generating...' : 'Generate AI Response'}
              </button>
              <button
                onClick={improveAIResponse}
                disabled={improvingAI || !replyText.trim()}
                className="flex-1 px-4 py-2 bg-purple-100 text-purple-700 rounded-lg hover:bg-purple-200 transition-colors flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <RefreshCw className={`w-4 h-4 ${improvingAI ? 'animate-spin' : ''}`} />
                {improvingAI ? 'Improving...' : 'Improve with AI'}
              </button>
            </div>

            {/* Reply Input */}
            <div className="mb-4">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Your Response
              </label>
              <textarea
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
                rows={6}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent"
                placeholder="Write your response or click 'Generate AI Response'..."
              />
              <div className="flex items-center justify-between mt-1">
                <div className="text-sm text-gray-500">
                  {replyText.length} characters
                </div>
                {selectedReview.aiSuggestedResponse && (
                  <div className="flex items-center gap-1 text-xs text-purple-600">
                    <Sparkles className="w-3 h-3" />
                    <span>AI-powered</span>
                  </div>
                )}
              </div>
            </div>

            {/* Modal Actions */}
            <div className="flex gap-3 justify-end">
              <button
                onClick={() => {
                  setShowReplyModal(false);
                  setReplyText('');
                  setSelectedReview(null);
                }}
                className="px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={submitReply}
                disabled={!replyText.trim()}
                className="btn-primary flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Send className="w-4 h-4" />
                Send Reply
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
