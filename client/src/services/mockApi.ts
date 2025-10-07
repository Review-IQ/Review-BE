import {
  mockUser,
  mockBusinesses,
  mockReviews,
  mockPlatforms,
  mockCustomers,
  mockCampaigns,
  mockCompetitors,
  mockAnalytics
} from './mockData';

// Simulate API delay
const delay = (ms: number = 300) => new Promise(resolve => setTimeout(resolve, ms));

// Location names for generating data
const LOCATION_NAMES = {
  1: 'Downtown Store',
  2: 'Santa Monica Beach',
  3: 'Beverly Hills',
  4: 'San Francisco',
  5: 'Portland'
};

class MockApiService {
  // Auth endpoints
  async register(data: { email: string; fullName: string }) {
    await delay();
    console.log('[MOCK API] Register:', data);
    return { data: { user: mockUser, message: 'Registration successful' } };
  }

  async getCurrentUser() {
    await delay();
    console.log('[MOCK API] Get current user');
    return { data: mockUser };
  }

  async updateProfile(data: { fullName?: string; email?: string }) {
    await delay();
    console.log('[MOCK API] Update profile:', data);
    return { data: { ...mockUser, ...data } };
  }

  // Business endpoints
  async getBusinesses() {
    await delay();
    console.log('[MOCK API] Get businesses');
    return { data: mockBusinesses };
  }

  async getBusiness(id: number) {
    await delay();
    console.log('[MOCK API] Get business:', id);
    const business = mockBusinesses.find(b => b.id === id);
    if (!business) throw new Error('Business not found');
    return { data: business };
  }

  async createBusiness(data: any) {
    await delay();
    console.log('[MOCK API] Create business:', data);
    const newBusiness = {
      id: mockBusinesses.length + 1,
      ...data,
      platformConnectionsCount: 0,
      reviewsCount: 0,
      avgRating: 0,
      createdAt: new Date().toISOString()
    };
    return { data: newBusiness };
  }

  async updateBusiness(id: number, data: any) {
    await delay();
    console.log('[MOCK API] Update business:', id, data);
    const business = mockBusinesses.find(b => b.id === id);
    if (!business) throw new Error('Business not found');
    return { data: { ...business, ...data } };
  }

  async deleteBusiness(id: number) {
    await delay();
    console.log('[MOCK API] Delete business:', id);
    return { data: { message: 'Business deleted successfully' } };
  }

  // Reviews endpoints
  async getReviews(params?: any) {
    await delay();
    console.log('[MOCK API] Get reviews with locationId:', params?.locationId);

    // Generate location-specific reviews
    const generateLocationReviews = (locationId: number) => {
      const locationName = (LOCATION_NAMES as any)[locationId] || 'Unknown Location';
      const baseCount = [15, 10, 12, 8, 5][locationId - 1] || 10;

      return Array.from({ length: baseCount }, (_, i) => {
        const customerName = `Customer ${locationId}-${i + 1}`;
        const responseText = i % 3 === 0 ? `Thank you for visiting ${locationName}!` : null;
        return {
          id: locationId * 100 + i,
          businessId: 1,
          businessName: 'Acme Restaurants',
          locationId: locationId,
          location: locationName,
          platform: ['Google', 'Yelp', 'Facebook'][i % 3],
          platformReviewId: `review-${locationId}-${i}`,
          reviewerName: customerName,
          reviewerAvatar: `https://ui-avatars.com/api/?name=${customerName.replace(' ', '+')}&background=random`,
          rating: [5, 5, 4, 5, 4, 3, 5, 4, 5, 4, 3, 5, 4, 2, 5][i] || 5,
          reviewText: `Great experience at ${locationName}! ${['Excellent service', 'Amazing food', 'Wonderful atmosphere', 'Highly recommend', 'Will come back'][i % 5]}!`,
          reviewDate: new Date(Date.now() - i * 86400000 * 2).toISOString(),
          sentiment: i % 8 === 0 ? 'Negative' : i % 4 === 0 ? 'Neutral' : 'Positive',
          sentimentScore: i % 8 === 0 ? 0.3 : i % 4 === 0 ? 0.6 : 0.9,
          isRead: i % 4 !== 0,
          isFlagged: i % 10 === 0,
          responseText,
          respondedAt: responseText ? new Date(Date.now() - i * 86400000 * 2 + 3600000).toISOString() : null,
          aiSuggestedResponse: `Thank you for your feedback about ${locationName}! We appreciate your visit and look forward to serving you again.`,
          reviewUrl: '#'
        };
      });
    };

    let filteredReviews: any[] = params?.locationId
      ? generateLocationReviews(params.locationId)
      : [...mockReviews];

    if (params?.businessId) {
      filteredReviews = filteredReviews.filter(r => r.businessId === params.businessId);
    }
    if (params?.platform) {
      filteredReviews = filteredReviews.filter(r => r.platform === params.platform);
    }
    if (params?.sentiment) {
      filteredReviews = filteredReviews.filter(r => r.sentiment === params.sentiment);
    }
    if (params?.rating) {
      filteredReviews = filteredReviews.filter(r => r.rating === params.rating);
    }
    if (params?.isRead !== undefined) {
      filteredReviews = filteredReviews.filter(r => r.isRead === params.isRead);
    }
    if (params?.isFlagged !== undefined) {
      filteredReviews = filteredReviews.filter(r => r.isFlagged === params.isFlagged);
    }

    // Pagination
    const page = params?.page || 1;
    const pageSize = params?.pageSize || 10;
    const totalCount = filteredReviews.length;
    const totalPages = Math.ceil(totalCount / pageSize);
    const startIndex = (page - 1) * pageSize;
    const paginatedReviews = filteredReviews.slice(startIndex, startIndex + pageSize);

    return {
      data: {
        reviews: paginatedReviews,
        page,
        pageSize,
        totalCount,
        totalPages
      }
    };
  }

  async getReview(id: number) {
    await delay();
    console.log('[MOCK API] Get review:', id);
    const review = mockReviews.find(r => r.id === id);
    if (!review) throw new Error('Review not found');
    return { data: review };
  }

  async replyToReview(id: number, responseText: string) {
    await delay();
    console.log('[MOCK API] Reply to review:', id, responseText);
    return { data: { message: 'Reply posted successfully' } };
  }

  async markReviewAsRead(id: number, isRead: boolean) {
    await delay();
    console.log('[MOCK API] Mark review as read:', id, isRead);
    return { data: { message: 'Review updated' } };
  }

  async flagReview(id: number, isFlagged: boolean) {
    await delay();
    console.log('[MOCK API] Flag review:', id, isFlagged);
    return { data: { message: 'Review flagged' } };
  }

  // Integrations endpoints
  async getAvailablePlatforms() {
    await delay();
    console.log('[MOCK API] Get available platforms');
    return { data: mockPlatforms };
  }

  async getBusinessConnections(businessId: number) {
    await delay();
    console.log('[MOCK API] Get business connections:', businessId);
    return { data: mockPlatforms.filter(p => p.isConnected) };
  }

  async initiateConnection(platform: string, businessId: number) {
    await delay();
    console.log('[MOCK API] Initiate connection:', platform, businessId);
    return { data: { authUrl: '#demo-mode', message: 'Demo mode - connection simulated' } };
  }

  async disconnectPlatform(connectionId: number) {
    await delay();
    console.log('[MOCK API] Disconnect platform:', connectionId);
    return { data: { message: 'Platform disconnected' } };
  }

  async syncPlatform(connectionId: number) {
    await delay();
    console.log('[MOCK API] Sync platform:', connectionId);
    return { data: { message: 'Sync completed', reviewsImported: 12 } };
  }

  // Analytics endpoints
  async getAnalytics(_params?: any) {
    await delay();
    console.log('[MOCK API] Get analytics:', _params);
    return { data: mockAnalytics };
  }

  async getDashboardSummary(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get dashboard summary:', _businessId, 'locationId:', locationId);

    // Location-specific data variations
    const locationData: Record<number, any> = {
      1: { // Downtown Store
        totalReviews: 342,
        averageRating: 4.7,
        unreadReviews: 8,
        thisMonthReviews: 45
      },
      2: { // Santa Monica Beach
        totalReviews: 198,
        averageRating: 4.8,
        unreadReviews: 5,
        thisMonthReviews: 32
      },
      3: { // Beverly Hills
        totalReviews: 267,
        averageRating: 4.6,
        unreadReviews: 11,
        thisMonthReviews: 28
      },
      4: { // San Francisco
        totalReviews: 156,
        averageRating: 4.5,
        unreadReviews: 7,
        thisMonthReviews: 19
      },
      5: { // Portland
        totalReviews: 89,
        averageRating: 4.9,
        unreadReviews: 3,
        thisMonthReviews: 12
      }
    };

    const data = locationId && locationData[locationId] ? locationData[locationId] : {
      totalReviews: 1052, // All locations combined
      averageRating: 4.7,
      unreadReviews: 34,
      thisMonthReviews: 136
    };

    return {
      data: {
        ...data,
        connectedPlatforms: 3,
        smsUsage: {
          sent: 7,
          limit: 10,
          remaining: 3
        },
        subscriptionPlan: 'Free',
        recentReviews: mockReviews.slice(0, 5).map(r => ({
          id: r.id,
          platform: r.platform,
          reviewerName: r.reviewerName,
          rating: r.rating,
          reviewDate: r.reviewDate
        }))
      }
    };
  }

  async getPlatformBreakdown(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get platform breakdown:', _businessId, 'locationId:', locationId);

    // Location-specific variations
    const locationMultiplier = locationId ? [1.2, 0.7, 0.9, 0.5, 0.3][locationId - 1] || 1 : 1;

    return {
      data: {
        platformBreakdown: [
          {
            platform: 'Google',
            totalReviews: Math.round(125 * locationMultiplier),
            averageRating: 4.6 + (locationId === 5 ? 0.3 : locationId === 2 ? 0.2 : 0),
            positiveCount: Math.round(95 * locationMultiplier),
            neutralCount: Math.round(22 * locationMultiplier),
            negativeCount: Math.round(8 * locationMultiplier)
          },
          {
            platform: 'Yelp',
            totalReviews: Math.round(82 * locationMultiplier),
            averageRating: 4.4 + (locationId === 5 ? 0.3 : locationId === 2 ? 0.2 : 0),
            positiveCount: Math.round(61 * locationMultiplier),
            neutralCount: Math.round(16 * locationMultiplier),
            negativeCount: Math.round(5 * locationMultiplier)
          },
          {
            platform: 'Facebook',
            totalReviews: Math.round(62 * locationMultiplier),
            averageRating: 4.5 + (locationId === 5 ? 0.3 : locationId === 2 ? 0.2 : 0),
            positiveCount: Math.round(48 * locationMultiplier),
            neutralCount: Math.round(11 * locationMultiplier),
            negativeCount: Math.round(3 * locationMultiplier)
          }
        ]
      }
    };
  }

  async getAnalyticsOverview(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get analytics overview:', _businessId, 'locationId:', locationId);

    // Location-specific data
    const locationData: Record<number, any> = {
      1: { // Downtown Store
        totalReviews: 342,
        averageRating: 4.7,
        responseRate: 85,
        sentimentBreakdown: { positive: 280, neutral: 45, negative: 17 },
        thisMonthReviews: 45,
        monthlyChange: 15.2
      },
      2: { // Santa Monica Beach
        totalReviews: 198,
        averageRating: 4.8,
        responseRate: 92,
        sentimentBreakdown: { positive: 172, neutral: 22, negative: 4 },
        thisMonthReviews: 32,
        monthlyChange: 8.7
      },
      3: { // Beverly Hills
        totalReviews: 267,
        averageRating: 4.6,
        responseRate: 76,
        sentimentBreakdown: { positive: 215, neutral: 38, negative: 14 },
        thisMonthReviews: 28,
        monthlyChange: -3.5
      },
      4: { // San Francisco
        totalReviews: 156,
        averageRating: 4.5,
        responseRate: 81,
        sentimentBreakdown: { positive: 118, neutral: 28, negative: 10 },
        thisMonthReviews: 19,
        monthlyChange: 22.1
      },
      5: { // Portland
        totalReviews: 89,
        averageRating: 4.9,
        responseRate: 95,
        sentimentBreakdown: { positive: 82, neutral: 6, negative: 1 },
        thisMonthReviews: 12,
        monthlyChange: 33.3
      }
    };

    const data = locationId && locationData[locationId] ? locationData[locationId] : {
      totalReviews: 1052,
      averageRating: 4.7,
      responseRate: 85,
      sentimentBreakdown: { positive: 867, neutral: 139, negative: 46 },
      thisMonthReviews: 136,
      monthlyChange: 14.7
    };

    return { data };
  }

  // POS Automation / SMS endpoints
  async getCustomers(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get customers:', _businessId, 'locationId:', locationId);

    const generateLocationCustomers = (locId: number) => {
      const locationName = (LOCATION_NAMES as any)[locId] || 'Unknown';
      const customerCount = [12, 8, 15, 10, 6][locId - 1] || 10;

      return Array.from({ length: customerCount }, (_, i) => ({
        id: locId * 100 + i + 1,
        name: `Customer ${String.fromCharCode(65 + i)} (${locationName})`,
        email: `customer${locId}${i + 1}@example.com`,
        phone: `+1 (555) ${locId}${i}${i}-${(i + 1) * 111}`,
        lastVisit: new Date(Date.now() - i * 86400000 * (locId + 1)).toISOString(),
        totalVisits: Math.max(1, customerCount - i),
        averageSpend: 35 + (locId * 5) + (i * 2.5)
      }));
    };

    const customers = locationId ? generateLocationCustomers(locationId) : mockCustomers;
    return { data: customers };
  }

  async getCampaigns(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get campaigns:', _businessId, 'locationId:', locationId);

    const generateLocationCampaigns = (locId: number) => {
      const locationName = (LOCATION_NAMES as any)[locId] || 'Unknown';
      const recipientBase = [150, 98, 134, 87, 52][locId - 1] || 100;

      return [
        {
          id: locId * 10 + 1,
          name: `${locationName} Weekend Promotion`,
          message: `Thanks for dining with us at ${locationName}! Enjoy ${15 + locId * 5}% off your next visit this weekend. Show this text to redeem.`,
          status: 'Sent',
          sentAt: new Date(Date.now() - 86400000 * locId).toISOString(),
          recipientCount: recipientBase,
          responseRate: 0.25 + (locId * 0.05)
        },
        {
          id: locId * 10 + 2,
          name: `${locationName} Review Request`,
          message: `We hope you enjoyed your recent visit to ${locationName}! We'd love to hear your feedback. Please leave us a review.`,
          status: locId <= 2 ? 'Scheduled' : 'Draft',
          scheduledFor: new Date(Date.now() + 86400000 * (7 - locId)).toISOString(),
          recipientCount: Math.round(recipientBase * 0.6),
          responseRate: null
        },
        {
          id: locId * 10 + 3,
          name: `${locationName} ${locId === 2 ? 'Beach Special' : locId === 3 ? 'Luxury Experience' : locId === 5 ? 'Farm Fresh Menu' : 'Loyalty Rewards'}`,
          message: locId === 2
            ? 'Join us for sunset dining at our beachside location! Reserve your table now.'
            : locId === 3
            ? 'Experience our new tasting menu at Beverly Hills. Limited seating available.'
            : locId === 5
            ? 'Try our new farm-to-table seasonal menu featuring local Oregon ingredients!'
            : 'Earn points with every visit! Join our loyalty program today.',
          status: 'Draft',
          scheduledFor: null,
          recipientCount: Math.round(recipientBase * 0.4),
          responseRate: null
        }
      ];
    };

    const campaigns = locationId ? generateLocationCampaigns(locationId) : mockCampaigns;
    return { data: campaigns };
  }

  async createCampaign(data: any) {
    await delay(500);
    console.log('[MOCK API] Create campaign:', data);
    const newCampaign = {
      id: mockCampaigns.length + 1,
      ...data,
      status: 'Scheduled',
      recipientCount: 0,
      responseRate: null
    };
    return { data: newCampaign };
  }

  async sendMessage(data: any) {
    await delay(500);
    console.log('[MOCK API] Send message:', data);
    return { data: { message: 'Messages sent successfully', sentCount: data.customerIds.length } };
  }

  // Competitors endpoints
  async getCompetitors(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get competitors:', _businessId, 'locationId:', locationId);

    // Generate location-specific competitors
    const generateLocationCompetitors = (locId: number) => {
      const locationName = (LOCATION_NAMES as any)[locId] || 'Unknown';
      const multiplier = [1.2, 0.8, 1.0, 0.6, 0.4][locId - 1] || 1.0;

      return [
        {
          id: locId * 10 + 1,
          name: `Competitor A near ${locationName}`,
          platform: 'Google',
          platformUrl: `https://google.com/maps/place/competitor-a-${locId}`,
          totalReviews: Math.round(1523 * multiplier),
          avgRating: 4.6 + (locId === 5 ? -0.3 : locId === 2 ? 0.1 : 0),
          ratingTrend: 2.3 * multiplier,
          responseRate: 92.5 - (locId * 3),
          avgResponseTime: 3.8 + (locId * 0.5),
          lastUpdated: new Date().toISOString(),
          sentiment: {
            positive: Math.round(78 * multiplier),
            neutral: Math.round(16 * multiplier),
            negative: Math.round(6 * multiplier)
          },
          reviewDistribution: [
            { rating: 5, count: Math.round(892 * multiplier) },
            { rating: 4, count: Math.round(423 * multiplier) },
            { rating: 3, count: Math.round(145 * multiplier) },
            { rating: 2, count: Math.round(42 * multiplier) },
            { rating: 1, count: Math.round(21 * multiplier) }
          ],
          recentReviewCount: Math.round(167 * multiplier)
        },
        {
          id: locId * 10 + 2,
          name: `Competitor B near ${locationName}`,
          platform: 'Yelp',
          platformUrl: `https://yelp.com/biz/competitor-b-${locId}`,
          totalReviews: Math.round(2341 * multiplier),
          avgRating: 4.8 + (locId === 1 ? -0.2 : locId === 4 ? -0.4 : 0),
          ratingTrend: -1.2 * multiplier,
          responseRate: 88.3 - (locId * 2),
          avgResponseTime: 2.5 + (locId * 0.3),
          lastUpdated: new Date().toISOString(),
          sentiment: {
            positive: Math.round(85 * multiplier),
            neutral: Math.round(11 * multiplier),
            negative: Math.round(4 * multiplier)
          },
          reviewDistribution: [
            { rating: 5, count: Math.round(1456 * multiplier) },
            { rating: 4, count: Math.round(623 * multiplier) },
            { rating: 3, count: Math.round(189 * multiplier) },
            { rating: 2, count: Math.round(52 * multiplier) },
            { rating: 1, count: Math.round(21 * multiplier) }
          ],
          recentReviewCount: Math.round(203 * multiplier)
        },
        {
          id: locId * 10 + 3,
          name: `Competitor C near ${locationName}`,
          platform: 'Google',
          platformUrl: `https://google.com/maps/place/competitor-c-${locId}`,
          totalReviews: Math.round(876 * multiplier),
          avgRating: 4.3 + (locId === 3 ? 0.2 : locId === 5 ? -0.5 : 0),
          ratingTrend: 0.8 * multiplier,
          responseRate: 75.6 - (locId * 4),
          avgResponseTime: 5.2 + (locId * 0.7),
          lastUpdated: new Date().toISOString(),
          sentiment: {
            positive: Math.round(68 * multiplier),
            neutral: Math.round(22 * multiplier),
            negative: Math.round(10 * multiplier)
          },
          reviewDistribution: [
            { rating: 5, count: Math.round(423 * multiplier) },
            { rating: 4, count: Math.round(298 * multiplier) },
            { rating: 3, count: Math.round(112 * multiplier) },
            { rating: 2, count: Math.round(32 * multiplier) },
            { rating: 1, count: Math.round(11 * multiplier) }
          ],
          recentReviewCount: Math.round(98 * multiplier)
        }
      ];
    };

    const competitors = locationId ? generateLocationCompetitors(locationId) : mockCompetitors;
    return { data: competitors };
  }

  async addCompetitor(data: any) {
    await delay();
    console.log('[MOCK API] Add competitor:', data);
    const newCompetitor = {
      id: mockCompetitors.length + 1,
      ...data,
      averageRating: 0,
      totalReviews: 0,
      recentReviewsCount: 0,
      lastUpdated: new Date().toISOString()
    };
    return { data: newCompetitor };
  }

  async removeCompetitor(id: number) {
    await delay();
    console.log('[MOCK API] Remove competitor:', id);
    return { data: { message: 'Competitor removed' } };
  }

  // Notifications endpoints
  async getNotifications(params?: { unreadOnly?: boolean; page?: number; pageSize?: number }) {
    await delay();
    console.log('[MOCK API] Get notifications:', params);

    const mockNotifications = [
      {
        id: 1,
        type: 0,
        title: 'New Google Review',
        message: 'John Smith left a 5-star review',
        data: JSON.stringify({ reviewId: 1, businessId: 1, platform: 'Google' }),
        isRead: false,
        createdAt: new Date(Date.now() - 1000 * 60 * 30).toISOString(), // 30 min ago
        readAt: null
      },
      {
        id: 2,
        type: 2,
        title: '⚠️ Low Rating Alert - Yelp',
        message: 'Jane Doe left a 2-star review',
        data: JSON.stringify({ reviewId: 2, businessId: 1, platform: 'Yelp' }),
        isRead: false,
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 2).toISOString(), // 2 hours ago
        readAt: null
      },
      {
        id: 3,
        type: 0,
        title: 'New Facebook Review',
        message: 'Sarah Johnson left a 4-star review',
        data: JSON.stringify({ reviewId: 3, businessId: 1, platform: 'Facebook' }),
        isRead: true,
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(), // 1 day ago
        readAt: new Date(Date.now() - 1000 * 60 * 60 * 12).toISOString()
      }
    ];

    let filtered = params?.unreadOnly ? mockNotifications.filter(n => !n.isRead) : mockNotifications;

    return {
      data: {
        notifications: filtered,
        totalCount: filtered.length,
        page: params?.page || 1,
        pageSize: params?.pageSize || 50,
        totalPages: 1
      }
    };
  }

  async getUnreadNotificationCount() {
    await delay(100);
    console.log('[MOCK API] Get unread notification count');
    return { data: { count: 2 } };
  }

  async markNotificationAsRead(id: number) {
    await delay();
    console.log('[MOCK API] Mark notification as read:', id);
    return { data: { message: 'Notification marked as read' } };
  }

  async markAllNotificationsAsRead() {
    await delay();
    console.log('[MOCK API] Mark all notifications as read');
    return { data: { message: 'Marked 2 notifications as read' } };
  }

  async deleteNotification(id: number) {
    await delay();
    console.log('[MOCK API] Delete notification:', id);
    return { data: null };
  }

  async getNotificationPreferences() {
    await delay();
    console.log('[MOCK API] Get notification preferences');
    return {
      data: {
        id: 1,
        userId: 1,
        emailNotifications: true,
        pushNotifications: false,
        smsNotifications: false,
        notifyOnNewReview: true,
        notifyOnReviewReply: true,
        notifyOnLowRating: true
      }
    };
  }

  async updateNotificationPreferences(preferences: any) {
    await delay();
    console.log('[MOCK API] Update notification preferences:', preferences);
    return { data: preferences };
  }

  // Team endpoints
  async getTeamMembers(_businessId: number) {
    await delay();
    console.log('[MOCK API] Get team members:', _businessId);
    return {
      data: [
        {
          id: 1,
          userId: 1,
          user: { id: 1, fullName: 'John Doe (You)', email: 'john@example.com' },
          role: 'Owner',
          joinedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 365).toISOString(),
          isActive: true
        },
        {
          id: 2,
          userId: 2,
          user: { id: 2, fullName: 'Jane Smith', email: 'jane@example.com' },
          role: 'Admin',
          joinedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 90).toISOString(),
          isActive: true
        },
        {
          id: 3,
          userId: 3,
          user: { id: 3, fullName: 'Bob Johnson', email: 'bob@example.com' },
          role: 'Member',
          joinedAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 30).toISOString(),
          isActive: true
        }
      ]
    };
  }

  async getPendingInvitations(_businessId: number) {
    await delay();
    console.log('[MOCK API] Get pending invitations:', _businessId);
    return {
      data: [
        {
          id: 1,
          email: 'newuser@example.com',
          role: 'Member',
          invitedBy: { fullName: 'John Doe' },
          createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24 * 2).toISOString(),
          expiresAt: new Date(Date.now() + 1000 * 60 * 60 * 24 * 5).toISOString(),
          status: 0
        }
      ]
    };
  }

  async inviteUser(_businessId: number, email: string, role: string) {
    await delay();
    console.log('[MOCK API] Invite user:', email, role);
    return {
      data: {
        message: 'Invitation sent successfully',
        invitation: {
          id: Date.now(),
          email,
          role,
          createdAt: new Date().toISOString(),
          expiresAt: new Date(Date.now() + 1000 * 60 * 60 * 24 * 7).toISOString()
        }
      }
    };
  }

  async revokeInvitation(invitationId: number) {
    await delay();
    console.log('[MOCK API] Revoke invitation:', invitationId);
    return { data: { message: 'Invitation revoked successfully' } };
  }

  async removeTeamMember(_businessId: number, userId: number) {
    await delay();
    console.log('[MOCK API] Remove team member:', userId);
    return { data: { message: 'Team member removed successfully' } };
  }

  async updateMemberRole(_businessId: number, userId: number, role: string) {
    await delay();
    console.log('[MOCK API] Update member role:', userId, role);
    return { data: { message: 'Role updated successfully' } };
  }

  // Invitation & Registration endpoints
  async getInvitationDetails(token: string) {
    await delay();
    console.log('[MOCK API] Get invitation details:', token);
    return {
      data: {
        email: 'newuser@example.com',
        businessName: 'Demo Restaurant',
        role: 'Member',
        inviterName: 'John Doe'
      }
    };
  }

  async acceptInvitationAndRegister(token: string, data: any) {
    await delay();
    console.log('[MOCK API] Accept invitation and register:', token, data);
    return { data: { message: 'Registration successful' } };
  }

  // AI endpoints
  async getAnalyticsInsights(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get analytics insights, locationId:', locationId);

    const locationName = locationId ? (LOCATION_NAMES as any)[locationId] || 'Unknown' : 'All Locations';
    const ratingMap: Record<number, number> = { 1: 4.7, 2: 4.8, 3: 4.6, 4: 4.5, 5: 4.9 };
    const rating = locationId ? ratingMap[locationId] || 4.5 : 4.5;
    const responseRate = locationId ? 85 + (locationId * 2) : 85;
    const responseTime = locationId ? 2 + (locationId * 0.3) : 2;
    const volumeIncrease = locationId ? 23 + (locationId * 5) : 23;
    const sentimentIncrease = locationId ? 15 + (locationId * 3) : 15;

    return {
      data: {
        insights: `**Performance Analysis - ${locationName}**

Your business is showing strong performance trends with several key highlights:

**Key Metrics:**
• Average rating of ${rating}/5 stars demonstrates excellent customer satisfaction
• Response rate of ${responseRate}% shows good engagement with customer feedback
• Average response time of ${responseTime.toFixed(1)} hours is ${responseTime < 3 ? 'better than' : 'near'} industry standard

**Trends Identified:**
• Review volume increased ${volumeIncrease}% over the last 30 days
• Positive sentiment up ${sentimentIncrease}% compared to previous period
• Peak review activity occurs on weekends (Friday-Sunday)

**Areas of Concern:**
• ${locationId === 1 ? '15%' : locationId === 4 ? '18%' : '12%'} of reviews mention wait times - consider staffing adjustments
• Response time to negative reviews averages ${responseTime + 4} hours - target under 4 hours

**Growth Opportunities:**
• Strong 5-star reviews indicate satisfied customers - encourage social sharing
• Positive feedback about staff quality - highlight team in marketing
• ${locationId === 2 ? 'Beachside location attracts tourists' : locationId === 5 ? 'Portland market has growth potential' : 'Weekend traffic is high'} - consider ${locationId === 2 ? 'seasonal promotions' : locationId === 5 ? 'local partnerships' : 'extended hours or special promotions'}`
      }
    };
  }

  async getCompetitorInsights(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get competitor insights, locationId:', locationId);

    const locationName = locationId ? (LOCATION_NAMES as any)[locationId] || 'Unknown' : 'All Locations';
    const ratingMap: Record<number, number> = { 1: 4.7, 2: 4.8, 3: 4.6, 4: 4.5, 5: 4.9 };
    const myRating = locationId ? ratingMap[locationId] || 4.5 : 4.5;
    const myReviews = locationId ? [342, 198, 267, 156, 89][locationId - 1] || 167 : 167;
    const topCompetitorRating = locationId ? 4.7 + (locationId === 2 ? 0.1 : locationId === 5 ? -0.4 : 0) : 4.7;
    const topCompetitorReviews = locationId ? [523, 412, 445, 289, 134][locationId - 1] || 342 : 342;
    const marketAvg = locationId ? 4.2 + (locationId * 0.05) : 4.2;
    const ranking = locationId === 2 || locationId === 5 ? 'top 15%' : locationId === 1 ? 'top 20%' : 'top 25%';

    return {
      data: {
        insights: `**Competitive Position Analysis - ${locationName}**

**Your Market Standing:**
Your business currently ranks in the ${ranking} of local competitors based on customer ratings and review volume.

**Competitive Strengths:**
• Your ${myRating}-star average is ${(myRating - marketAvg).toFixed(1)} stars above market average
• Review response rate of ${85 + (locationId || 0) * 2}% vs competitor average of 62%
• ${locationId === 2 ? 'Prime beachfront location' : locationId === 5 ? 'Strong local community presence' : 'Faster response times'} give you a competitive edge
• Customer service consistently praised in reviews

**Competitor Benchmarks:**
• Top competitor: "${locationId === 1 ? 'Downtown Bistro' : locationId === 2 ? 'Beachside Grill' : locationId === 3 ? 'Rodeo Drive Cafe' : locationId === 4 ? 'Bay Area Eatery' : 'Portland Brunch Spot'}" - ${topCompetitorRating} stars (${topCompetitorReviews} reviews)
• Market average: ${marketAvg.toFixed(1)} stars
• Your business: ${myRating} stars (${myReviews} reviews)

**Key Differentiators:**
• ${locationId === 2 ? 'Ocean view dining experience' : locationId === 5 ? 'Farm-to-table local sourcing' : 'Competitors struggle with consistency'} - you excel here
• Your response quality is superior to ${70 + (locationId || 0) * 3}% of competitors
• Unique strength: ${locationId === 3 ? 'luxury ambiance' : locationId === 4 ? 'diverse menu options' : 'personalized service'} (mentioned ${45 + (locationId || 0) * 5}% more often)

**Opportunities to Gain Edge:**
• ${topCompetitorReviews > myReviews ? `Increase review volume - you're behind top competitor by ${topCompetitorReviews - myReviews} reviews` : 'Maintain your review volume leadership'}
• Promote your ${locationId === 2 ? 'beachfront advantage' : locationId === 5 ? 'sustainability practices' : 'faster response times'} in marketing
• Competitors weak on ${locationId === 1 ? 'downtown parking' : locationId === 4 ? 'wait times' : 'weekend service'} - capitalize on this strength
• Consider ${locationId === 3 ? 'VIP membership' : 'loyalty'} program - competitors lack this feature

**Recommendations:**
1. Focus on ${topCompetitorReviews > myReviews ? 'getting more reviews to boost visibility' : 'maintaining review quality'}
2. Highlight your ${locationId === 2 ? 'location' : locationId === 5 ? 'local sourcing' : 'response time'} advantage
3. Market your ${locationId === 1 ? 'accessibility' : locationId === 4 ? 'consistency' : 'weekend reliability'}
4. Maintain service quality consistency`
      }
    };
  }

  async getReviewSummary(_businessId: number, days: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get review summary for', days, 'days, locationId:', locationId);

    const locationName = locationId ? (LOCATION_NAMES as any)[locationId] || 'Unknown' : 'All Locations';
    const reviewCount = locationId ? [47, 32, 41, 25, 14][locationId - 1] || 47 : 47;
    const serviceMentions = locationId ? [67, 52, 71, 43, 38][locationId - 1] || 67 : 67;
    const foodMentions = locationId ? [54, 61, 48, 39, 42][locationId - 1] || 54 : 54;
    const atmosphereMentions = locationId ? [42, 58, 55, 31, 29][locationId - 1] || 42 : 42;
    const valueMentions = locationId ? [38, 29, 22, 41, 47][locationId - 1] || 38 : 38;
    const waitMentions = locationId ? [18, 12, 22, 25, 8][locationId - 1] || 18 : 18;
    const parkingMentions = locationId ? [12, 6, 19, 15, 5][locationId - 1] || 12 : 12;
    const menuMentions = locationId ? [8, 14, 6, 11, 18][locationId - 1] || 8 : 8;

    const locationSpecificFeedback = locationId === 1
      ? "Perfect downtown location! Easy to get to and the ambiance is wonderful for business lunches."
      : locationId === 2
      ? "The ocean view is absolutely stunning! Best beachside dining experience we've had. Sunset timing was perfect!"
      : locationId === 3
      ? "Truly luxurious experience! The attention to detail and upscale atmosphere exceeded all expectations."
      : locationId === 4
      ? "Great variety on the menu and the city views are amazing. Perfect spot for tourists and locals alike."
      : "Love the farm-to-table concept and local ingredients! You can really taste the freshness in every dish.";

    return {
      data: {
        summary: `**Review Summary - Last ${days} Days (${locationName})**

**Overall Sentiment: Positive** ⭐⭐⭐⭐${locationId === 2 || locationId === 5 ? '½+' : '½'}

**Key Positive Themes:**
• **Excellent Service (mentioned ${serviceMentions} times)** - Staff consistently praised for friendliness and attentiveness
• **Food Quality (mentioned ${foodMentions} times)** - Dishes described as "fresh", "delicious", and "perfectly prepared"
• **Atmosphere (mentioned ${atmosphereMentions} times)** - Ambiance rated as "${locationId === 2 ? 'breathtaking ocean view' : locationId === 3 ? 'sophisticated and elegant' : 'cozy'}", "${locationId === 1 ? 'professional yet welcoming' : 'welcoming'}", and "perfect for ${locationId === 3 ? 'special occasions' : 'dates'}"
• **Value (mentioned ${valueMentions} times)** - Customers feel prices are ${locationId === 3 ? 'justified for the luxury experience' : locationId === 5 ? 'great for organic quality' : 'reasonable for quality received'}

**Common Concerns:**
• **Wait Times (mentioned ${waitMentions} times)** - ${waitMentions > 20 ? 'Frequent mentions of' : 'Some customers experienced'} longer waits during peak hours
• **Parking (mentioned ${parkingMentions} times)** - ${parkingMentions > 15 ? 'Significant' : 'Limited'} parking availability ${locationId === 1 ? 'in downtown area' : locationId === 3 ? 'on Rodeo Drive' : 'mentioned by several guests'}
• **Menu Variety (mentioned ${menuMentions} times)** - ${menuMentions > 12 ? 'Multiple requests' : 'A few requests'} for more ${locationId === 5 ? 'traditional options alongside organic choices' : 'vegetarian/vegan options'}

**Most Frequent Keywords:**
"${locationId === 2 ? 'Ocean view' : locationId === 3 ? 'Elegant' : locationId === 5 ? 'Fresh' : 'Amazing'}", "Friendly", "Delicious", "Recommend", "${locationId === 1 ? 'Professional' : locationId === 4 ? 'Diverse' : 'Great experience'}", "Will return"

**Notable Standout Feedback:**
"${locationSpecificFeedback}"

**Customer Behavior Patterns:**
• ${73 + (locationId || 0) * 2}% of reviews mention plans to return
• ${89 - (locationId === 4 ? 5 : 0)}% would recommend to friends
• ${locationId === 3 ? 'Couples celebrating special occasions' : locationId === 2 ? 'Tourists and locals' : locationId === 1 ? 'Business professionals and couples' : 'Couples and small groups'} most common customer type
• Peak positive reviews on ${locationId === 1 ? 'weekday lunches and' : ''}Friday/Saturday evenings`,
        reviewCount,
        period: `${days} days`
      }
    };
  }

  async getActionableRecommendations(_businessId: number, locationId?: number | null) {
    await delay();
    console.log('[MOCK API] Get actionable recommendations, locationId:', locationId);

    const baseRecommendations = [
      `Respond to all reviews within 24 hours to maintain 90%+ response rate and improve customer engagement${locationId === 5 ? ' - Portland customers especially value quick responses' : ''}`,
      `${locationId === 1 ? 'Implement valet parking service for downtown location to address the 12 parking concerns' : locationId === 3 ? 'Partner with Rodeo Drive parking structures to ease the 19 parking complaints' : locationId === 2 ? 'Promote nearby beach parking lots (parking complaints are low at 6)' : 'Address wait time concerns by implementing a reservation system or SMS notification when tables are ready'}`,
    ];

    const locationSpecific = locationId === 1
      ? [
          "Expand lunch special promotions to capture more business professionals in downtown area",
          "Leverage your professional atmosphere in LinkedIn and business-focused marketing",
          "Consider corporate catering packages for nearby office buildings"
        ]
      : locationId === 2
      ? [
          "Capitalize on ocean view by offering sunset dinner specials and beach wedding packages",
          "Create Instagram-worthy moments with beachside photo opportunities",
          "Develop seasonal menu items featuring fresh seafood from local catches"
        ]
      : locationId === 3
      ? [
          "Introduce VIP membership program for luxury clientele to increase repeat visits",
          "Partner with Beverly Hills hotels for exclusive dining referrals",
          "Enhance wine selection and sommelier services based on upscale customer expectations"
        ]
      : locationId === 4
      ? [
          "Reduce wait times during peak hours - implement queue management system (25 mentions)",
          "Promote menu diversity and international cuisine options in marketing",
          "Create tourist-friendly packages with city tour partnerships"
        ]
      : locationId === 5
      ? [
          "Expand farm-to-table partnerships with local Oregon farms based on positive feedback",
          "Balance organic menu with traditional options (18 requests for variety)",
          "Implement sustainability initiatives in marketing to attract eco-conscious Portland market"
        ]
      : [
          "Expand vegetarian and vegan menu options based on 8 customer requests in recent feedback",
          "Train weekend staff to maintain weekday service quality standards based on positive weekend feedback patterns",
          "Implement a loyalty program to encourage repeat visits from the 73% who mention plans to return"
        ];

    return {
      data: {
        recommendations: [...baseRecommendations, ...locationSpecific]
      }
    };
  }
}

export const mockApi = new MockApiService();
