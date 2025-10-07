// Mock data for demo mode

export const mockUser = {
  id: 1,
  email: 'demo@reviewhub.com',
  fullName: 'Demo User',
  companyName: 'Demo Restaurant Group',
  phoneNumber: '+1 (555) 123-4567',
  subscriptionPlan: 'Pro',
  subscriptionExpiresAt: '2025-12-31T23:59:59Z',
  createdAt: '2024-01-15T10:30:00Z'
};

export const mockBusinesses = [
  {
    id: 1,
    name: 'Main Street Cafe',
    industry: 'Restaurant',
    description: 'Cozy cafe serving artisanal coffee and fresh pastries',
    website: 'https://mainstreetcafe.com',
    phoneNumber: '+1 (555) 234-5678',
    address: '123 Main Street',
    city: 'New York',
    state: 'NY',
    zipCode: '10001',
    country: 'USA',
    logoUrl: 'https://ui-avatars.com/api/?name=Main+Street+Cafe&background=0284c7&color=fff',
    platformConnectionsCount: 4,
    reviewsCount: 247,
    avgRating: 4.5,
    createdAt: '2024-01-01T00:00:00Z'
  },
  {
    id: 2,
    name: 'Downtown Pizzeria',
    industry: 'Restaurant',
    description: 'Authentic New York style pizza',
    website: 'https://downtownpizza.com',
    phoneNumber: '+1 (555) 345-6789',
    address: '456 Broadway',
    city: 'New York',
    state: 'NY',
    zipCode: '10013',
    country: 'USA',
    logoUrl: 'https://ui-avatars.com/api/?name=Downtown+Pizzeria&background=ef4444&color=fff',
    platformConnectionsCount: 3,
    reviewsCount: 189,
    avgRating: 4.7,
    createdAt: '2024-02-01T00:00:00Z'
  }
];

export const mockReviews = [
  {
    id: 1,
    businessId: 1,
    platform: 'Google',
    reviewerName: 'Sarah Johnson',
    reviewerAvatar: 'https://ui-avatars.com/api/?name=Sarah+Johnson&background=random',
    rating: 5,
    reviewText: 'Amazing coffee and the staff is incredibly friendly! The atmosphere is perfect for getting work done or meeting friends.',
    reviewDate: '2024-03-15T14:30:00Z',
    sentiment: 'Positive',
    sentimentScore: 0.95,
    isRead: false,
    isFlagged: false,
    responseText: null,
    respondedAt: null,
    aiSuggestedResponse: 'Thank you so much for your kind words, Sarah! We\'re thrilled you enjoyed our coffee and atmosphere. We look forward to serving you again soon!'
  },
  {
    id: 2,
    businessId: 1,
    platform: 'Yelp',
    reviewerName: 'Mike Chen',
    reviewerAvatar: 'https://ui-avatars.com/api/?name=Mike+Chen&background=random',
    rating: 4,
    reviewText: 'Great pastries but the wait time can be a bit long during peak hours. Still worth it though!',
    reviewDate: '2024-03-14T10:15:00Z',
    sentiment: 'Mixed',
    sentimentScore: 0.65,
    isRead: true,
    isFlagged: false,
    responseText: 'Thanks for the feedback, Mike! We\'re working on improving our service during busy times.',
    respondedAt: '2024-03-14T16:00:00Z',
    aiSuggestedResponse: null
  },
  {
    id: 3,
    businessId: 2,
    platform: 'Google',
    reviewerName: 'Emily Rodriguez',
    reviewerAvatar: 'https://ui-avatars.com/api/?name=Emily+Rodriguez&background=random',
    rating: 5,
    reviewText: 'Best pizza in NYC! The crust is perfect and the ingredients are always fresh.',
    reviewDate: '2024-03-13T19:45:00Z',
    sentiment: 'Positive',
    sentimentScore: 0.92,
    isRead: true,
    isFlagged: false,
    responseText: null,
    respondedAt: null,
    aiSuggestedResponse: 'Thank you, Emily! We\'re proud to serve authentic NYC pizza. Come back soon!'
  },
  {
    id: 4,
    businessId: 1,
    platform: 'TripAdvisor',
    reviewerName: 'John Smith',
    reviewerAvatar: 'https://ui-avatars.com/api/?name=John+Smith&background=random',
    rating: 2,
    reviewText: 'Coffee was cold when it arrived and the service was slow.',
    reviewDate: '2024-03-12T11:20:00Z',
    sentiment: 'Negative',
    sentimentScore: 0.25,
    isRead: false,
    isFlagged: true,
    responseText: null,
    respondedAt: null,
    aiSuggestedResponse: 'We sincerely apologize for this experience, John. This doesn\'t meet our standards. Please contact us directly so we can make this right.'
  },
  {
    id: 5,
    businessId: 2,
    platform: 'Facebook',
    reviewerName: 'Lisa Anderson',
    reviewerAvatar: 'https://ui-avatars.com/api/?name=Lisa+Anderson&background=random',
    rating: 5,
    reviewText: 'Family-owned gem! The owners are so welcoming and the pizza reminds me of my trip to Italy.',
    reviewDate: '2024-03-10T18:30:00Z',
    sentiment: 'Positive',
    sentimentScore: 0.98,
    isRead: true,
    isFlagged: false,
    responseText: null,
    respondedAt: null,
    aiSuggestedResponse: null
  }
];

export const mockPlatforms = [
  {
    platform: 'Google',
    displayName: 'Google Business Profile',
    description: 'Manage your Google reviews and business information',
    icon: 'https://www.google.com/favicon.ico',
    isConnected: true,
    lastSyncedAt: '2024-03-15T08:00:00Z'
  },
  {
    platform: 'Yelp',
    displayName: 'Yelp',
    description: 'Connect to Yelp for Business',
    icon: 'https://www.yelp.com/favicon.ico',
    isConnected: true,
    lastSyncedAt: '2024-03-15T08:00:00Z'
  },
  {
    platform: 'Facebook',
    displayName: 'Facebook',
    description: 'Manage Facebook Page reviews',
    icon: 'https://www.facebook.com/favicon.ico',
    isConnected: false,
    lastSyncedAt: null
  },
  {
    platform: 'TripAdvisor',
    displayName: 'TripAdvisor',
    description: 'Monitor TripAdvisor reviews',
    icon: 'https://www.tripadvisor.com/favicon.ico',
    isConnected: true,
    lastSyncedAt: '2024-03-15T08:00:00Z'
  }
];

export const mockCustomers = [
  {
    id: 1,
    name: 'Alice Williams',
    email: 'alice@example.com',
    phone: '+1 (555) 111-2222',
    lastVisit: '2024-03-14T12:00:00Z',
    totalVisits: 12,
    averageSpend: 45.50
  },
  {
    id: 2,
    name: 'Bob Thompson',
    email: 'bob@example.com',
    phone: '+1 (555) 222-3333',
    lastVisit: '2024-03-13T14:30:00Z',
    totalVisits: 8,
    averageSpend: 38.75
  },
  {
    id: 3,
    name: 'Carol Davis',
    email: 'carol@example.com',
    phone: '+1 (555) 333-4444',
    lastVisit: '2024-03-10T19:00:00Z',
    totalVisits: 5,
    averageSpend: 52.00
  }
];

export const mockCampaigns = [
  {
    id: 1,
    name: 'Weekend Promotion',
    message: 'Thanks for dining with us! Enjoy 20% off your next visit this weekend. Show this text to redeem.',
    status: 'Sent',
    sentAt: '2024-03-14T10:00:00Z',
    recipientCount: 150,
    responseRate: 0.32
  },
  {
    id: 2,
    name: 'Review Request',
    message: 'We hope you enjoyed your recent visit! We\'d love to hear your feedback. Please leave us a review.',
    status: 'Scheduled',
    scheduledFor: '2024-03-16T09:00:00Z',
    recipientCount: 85,
    responseRate: null
  }
];

export const mockCompetitors = [
  {
    id: 1,
    name: 'Main Street Bistro',
    platform: 'Google',
    platformUrl: 'https://google.com/maps/place/main-street-bistro',
    totalReviews: 1523,
    avgRating: 4.6,
    ratingTrend: 2.3,
    responseRate: 92.5,
    avgResponseTime: 3.8,
    lastUpdated: new Date().toISOString(),
    sentiment: {
      positive: 78,
      neutral: 16,
      negative: 6
    },
    reviewDistribution: [
      { rating: 5, count: 892 },
      { rating: 4, count: 423 },
      { rating: 3, count: 145 },
      { rating: 2, count: 42 },
      { rating: 1, count: 21 }
    ],
    recentReviewCount: 167
  },
  {
    id: 2,
    name: 'Coffee Corner Cafe',
    platform: 'Yelp',
    platformUrl: 'https://yelp.com/biz/coffee-corner-cafe',
    totalReviews: 986,
    avgRating: 4.3,
    ratingTrend: -1.2,
    responseRate: 68.4,
    avgResponseTime: 8.5,
    lastUpdated: new Date().toISOString(),
    sentiment: {
      positive: 65,
      neutral: 24,
      negative: 11
    },
    reviewDistribution: [
      { rating: 5, count: 456 },
      { rating: 4, count: 312 },
      { rating: 3, count: 145 },
      { rating: 2, count: 52 },
      { rating: 1, count: 21 }
    ],
    recentReviewCount: 98
  },
  {
    id: 3,
    name: 'Urban Eatery',
    platform: 'Google',
    platformUrl: 'https://google.com/maps/place/urban-eatery',
    totalReviews: 2145,
    avgRating: 4.8,
    ratingTrend: 5.1,
    responseRate: 95.8,
    avgResponseTime: 2.1,
    lastUpdated: new Date().toISOString(),
    sentiment: {
      positive: 85,
      neutral: 11,
      negative: 4
    },
    reviewDistribution: [
      { rating: 5, count: 1523 },
      { rating: 4, count: 478 },
      { rating: 3, count: 98 },
      { rating: 2, count: 32 },
      { rating: 1, count: 14 }
    ],
    recentReviewCount: 234
  }
];

export const mockAnalytics = {
  reviewTrends: [
    { date: '2024-02-15', count: 12, avgRating: 4.5 },
    { date: '2024-02-22', count: 15, avgRating: 4.6 },
    { date: '2024-03-01', count: 18, avgRating: 4.4 },
    { date: '2024-03-08', count: 22, avgRating: 4.7 },
    { date: '2024-03-15', count: 19, avgRating: 4.5 }
  ],
  ratingDistribution: [
    { rating: 5, count: 156 },
    { rating: 4, count: 78 },
    { rating: 3, count: 23 },
    { rating: 2, count: 8 },
    { rating: 1, count: 4 }
  ],
  sentimentAnalysis: [
    { sentiment: 'Positive', count: 198, percentage: 74 },
    { sentiment: 'Neutral', count: 52, percentage: 19 },
    { sentiment: 'Negative', count: 19, percentage: 7 }
  ],
  platformBreakdown: [
    { platform: 'Google', count: 125, avgRating: 4.6 },
    { platform: 'Yelp', count: 82, avgRating: 4.4 },
    { platform: 'TripAdvisor', count: 45, avgRating: 4.5 },
    { platform: 'Facebook', count: 17, avgRating: 4.7 }
  ]
};
