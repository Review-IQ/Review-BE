import axios, { type AxiosInstance, type AxiosError } from 'axios';
import { mockApi } from './mockApi';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api';
const IS_DEMO_MODE = import.meta.env.VITE_DEMO_MODE === 'true';

// Token retrieval function - now just gets from localStorage
const getAccessToken = (): string | null => {
  return localStorage.getItem('access_token');
};

// Log demo mode status
if (IS_DEMO_MODE) {
  console.log('%c🎭 DEMO MODE ENABLED', 'background: #0284c7; color: white; padding: 4px 8px; border-radius: 4px; font-weight: bold;');
  console.log('%cAll API calls will use mock data. No backend or Auth0 required!', 'color: #0284c7;');
}

class ApiService {
  private client: AxiosInstance;

  constructor() {
    this.client = axios.create({
      baseURL: API_BASE_URL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Request interceptor to add auth token
    this.client.interceptors.request.use(
      (config) => {
        const token = getAccessToken();
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          // Unauthorized - redirect to login
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Auth endpoints
  async register(data: { email: string; fullName: string }) {
    if (IS_DEMO_MODE) return mockApi.register(data);
    return this.client.post('/auth/register', data);
  }

  async getCurrentUser() {
    if (IS_DEMO_MODE) return mockApi.getCurrentUser();
    return this.client.get('/auth/me');
  }

  async updateProfile(data: { fullName?: string; email?: string }) {
    if (IS_DEMO_MODE) return mockApi.updateProfile(data);
    return this.client.put('/auth/profile', data);
  }

  // Business endpoints
  async getBusinesses() {
    if (IS_DEMO_MODE) return mockApi.getBusinesses();
    return this.client.get('/businesses');
  }

  async getBusiness(id: number) {
    if (IS_DEMO_MODE) return mockApi.getBusiness(id);
    return this.client.get(`/businesses/${id}`);
  }

  async createBusiness(data: {
    name: string;
    industry?: string;
    description?: string;
    website?: string;
    phoneNumber?: string;
    address?: string;
    city?: string;
    state?: string;
    zipCode?: string;
    country?: string;
    logoUrl?: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.createBusiness(data);
    return this.client.post('/businesses', data);
  }

  async updateBusiness(id: number, data: Partial<{
    name: string;
    industry: string;
    description: string;
    website: string;
    phoneNumber: string;
    address: string;
    city: string;
    state: string;
    zipCode: string;
    country: string;
    logoUrl: string;
  }>) {
    if (IS_DEMO_MODE) return mockApi.updateBusiness(id, data);
    return this.client.put(`/businesses/${id}`, data);
  }

  async deleteBusiness(id: number) {
    if (IS_DEMO_MODE) return mockApi.deleteBusiness(id);
    return this.client.delete(`/businesses/${id}`);
  }

  // Reviews endpoints
  async getReviews(params?: {
    businessId?: number;
    locationId?: number;
    platform?: string;
    sentiment?: string;
    rating?: number;
    isRead?: boolean;
    isFlagged?: boolean;
    page?: number;
    pageSize?: number;
  }) {
    if (IS_DEMO_MODE) return mockApi.getReviews(params);
    return this.client.get('/reviews', { params });
  }

  async getReview(id: number) {
    if (IS_DEMO_MODE) return mockApi.getReview(id);
    return this.client.get(`/reviews/${id}`);
  }

  async replyToReview(id: number, responseText: string) {
    if (IS_DEMO_MODE) return mockApi.replyToReview(id, responseText);
    return this.client.post(`/reviews/${id}/reply`, { responseText });
  }

  async markReviewAsRead(id: number, isRead: boolean) {
    if (IS_DEMO_MODE) return mockApi.markReviewAsRead(id, isRead);
    return this.client.patch(`/reviews/${id}/read`, { isRead });
  }

  async flagReview(id: number, isFlagged: boolean) {
    if (IS_DEMO_MODE) return mockApi.flagReview(id, isFlagged);
    return this.client.patch(`/reviews/${id}/flag`, { isFlagged });
  }

  // Integrations endpoints
  async getAvailablePlatforms() {
    if (IS_DEMO_MODE) return mockApi.getAvailablePlatforms();
    return this.client.get('/integrations/platforms');
  }

  async getBusinessConnections(businessId: number) {
    if (IS_DEMO_MODE) return mockApi.getBusinessConnections(businessId);
    return this.client.get(`/integrations/business/${businessId}`);
  }

  async initiateConnection(platform: string, businessId: number) {
    if (IS_DEMO_MODE) return mockApi.initiateConnection(platform, businessId);
    return this.client.post(`/integrations/connect/${platform}`, { businessId });
  }

  async disconnectPlatform(connectionId: number) {
    if (IS_DEMO_MODE) return mockApi.disconnectPlatform(connectionId);
    return this.client.delete(`/integrations/${connectionId}`);
  }

  async syncPlatform(connectionId: number) {
    if (IS_DEMO_MODE) return mockApi.syncPlatform(connectionId);
    return this.client.post(`/integrations/${connectionId}/sync`);
  }

  // Analytics endpoints
  async getAnalyticsOverview(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getAnalyticsOverview(businessId, locationId);
    return this.client.get(`/analytics/overview/${businessId}`, { params: locationId ? { locationId } : {} });
  }

  async getRatingTrend(businessId: number, months: number = 6, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getAnalytics({ businessId });
    return this.client.get(`/analytics/rating-trend/${businessId}`, { params: { months, ...(locationId ? { locationId } : {}) } });
  }

  async getPlatformBreakdown(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getPlatformBreakdown(businessId, locationId);
    return this.client.get(`/analytics/platform-breakdown/${businessId}`, { params: locationId ? { locationId } : {} });
  }

  async getSentimentAnalysis(businessId: number, days: number = 30, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getAnalytics({ businessId });
    return this.client.get(`/analytics/sentiment-analysis/${businessId}`, { params: { days, ...(locationId ? { locationId } : {}) } });
  }

  async getTopKeywords(businessId: number, limit: number = 10) {
    if (IS_DEMO_MODE) return mockApi.getAnalytics({ businessId });
    return this.client.get(`/analytics/top-keywords/${businessId}`, { params: { limit } });
  }

  async getResponseTimeMetrics(businessId: number) {
    if (IS_DEMO_MODE) return mockApi.getAnalytics({ businessId });
    return this.client.get(`/analytics/response-time/${businessId}`);
  }

  async getDashboardSummary(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getDashboardSummary(businessId, locationId);
    return this.client.get(`/analytics/dashboard-summary/${businessId}`, { params: locationId ? { locationId } : {} });
  }

  // Customer endpoints
  async getCustomers(businessId: number, page: number = 1, pageSize: number = 50, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(businessId, locationId);
    const params: any = { page, pageSize };
    if (locationId) params.locationId = locationId;
    return this.client.get(`/customers/${businessId}`, { params });
  }

  async getCustomer(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(0);
    return this.client.get(`/customers/detail/${id}`);
  }

  async createCustomer(data: {
    businessId: number;
    name: string;
    email: string;
    phoneNumber: string;
    lastVisit?: string;
    totalVisits?: number;
    notes?: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(data.businessId);
    return this.client.post('/customers', data);
  }

  async updateCustomer(id: number, data: {
    name: string;
    email: string;
    phoneNumber: string;
    lastVisit?: string;
    totalVisits?: number;
    notes?: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(0);
    return this.client.put(`/customers/${id}`, data);
  }

  async deleteCustomer(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(0);
    return this.client.delete(`/customers/${id}`);
  }

  async recordVisit(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(0);
    return this.client.post(`/customers/${id}/record-visit`);
  }

  // Campaign endpoints
  async getCampaigns(businessId: number, page: number = 1, pageSize: number = 20, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getCampaigns(businessId, locationId);
    const params: any = { page, pageSize };
    if (locationId) params.locationId = locationId;
    return this.client.get(`/campaigns/${businessId}`, { params });
  }

  async getCampaign(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCampaigns(0);
    return this.client.get(`/campaigns/detail/${id}`);
  }

  async createCampaign(data: {
    businessId: number;
    name: string;
    message: string;
    scheduledFor?: string;
    recipientPhoneNumbers: string[];
  }) {
    if (IS_DEMO_MODE) return mockApi.createCampaign(data);
    return this.client.post('/campaigns', data);
  }

  async sendCampaign(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCampaigns(0);
    return this.client.post(`/campaigns/${id}/send`);
  }

  async updateCampaign(id: number, data: {
    name: string;
    message: string;
    scheduledFor?: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.getCampaigns(0);
    return this.client.put(`/campaigns/${id}`, data);
  }

  async deleteCampaign(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCampaigns(0);
    return this.client.delete(`/campaigns/${id}`);
  }

  // SMS endpoints
  async sendSms(data: {
    businessId: number;
    phoneNumber: string;
    message: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.sendMessage({ customerIds: [], message: data.message });
    return this.client.post('/sms/send', data);
  }

  async sendBulkSms(data: {
    businessId: number;
    phoneNumbers: string[];
    message: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.sendMessage({ customerIds: [], message: data.message });
    return this.client.post('/sms/send-bulk', data);
  }

  async getSmsMessages(businessId: number, page: number = 1, pageSize: number = 50) {
    if (IS_DEMO_MODE) return mockApi.getCustomers(businessId);
    return this.client.get(`/sms/messages/${businessId}`, { params: { page, pageSize } });
  }

  async getSmsUsage(businessId: number) {
    if (IS_DEMO_MODE) return { data: { plan: 'Free', sentThisMonth: 5, monthlyLimit: 10, remaining: 5 } };
    return this.client.get(`/sms/usage/${businessId}`);
  }

  // Competitor endpoints
  async getCompetitors(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getCompetitors(businessId, locationId);
    const params = locationId ? `?locationId=${locationId}` : '';
    return this.client.get(`/competitors/${businessId}${params}`);
  }

  async getCompetitor(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCompetitors(0);
    return this.client.get(`/competitors/detail/${id}`);
  }

  async addCompetitor(data: {
    businessId: number;
    name: string;
    platform: string;
    platformBusinessId: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.addCompetitor(data);
    return this.client.post('/competitors', data);
  }

  async updateCompetitor(id: number, data: {
    name: string;
    platformBusinessId: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.getCompetitors(0);
    return this.client.put(`/competitors/${id}`, data);
  }

  async deleteCompetitor(id: number) {
    if (IS_DEMO_MODE) return mockApi.removeCompetitor(id);
    return this.client.delete(`/competitors/${id}`);
  }

  async syncCompetitor(id: number) {
    if (IS_DEMO_MODE) return mockApi.getCompetitors(0);
    return this.client.post(`/competitors/${id}/sync`);
  }

  async getCompetitorComparison(businessId: number) {
    if (IS_DEMO_MODE) return mockApi.getCompetitors(businessId);
    return this.client.get(`/competitors/comparison/${businessId}`);
  }

  // Subscription endpoints
  async getSubscriptionPlans() {
    if (IS_DEMO_MODE) return { data: { plans: [
      { name: 'Free', price: 0, features: ['10 SMS/month', '1 business'] },
      { name: 'Pro', price: 49, priceId: 'price_pro', features: ['500 SMS/month', '5 businesses'] },
      { name: 'Enterprise', price: 149, priceId: 'price_ent', features: ['Unlimited SMS', 'Unlimited businesses'] }
    ]}};
    return this.client.get('/subscription/plans');
  }

  async createCheckoutSession(plan: string) {
    if (IS_DEMO_MODE) return { data: { url: '#' } };
    return this.client.post('/subscription/create-checkout-session', { plan });
  }

  async createPortalSession() {
    if (IS_DEMO_MODE) return { data: { url: '#' } };
    return this.client.post('/subscription/create-portal-session');
  }

  async getCurrentSubscription() {
    if (IS_DEMO_MODE) return { data: { plan: 'Free', expiresAt: null } };
    return this.client.get('/subscription/current');
  }

  async cancelSubscription() {
    if (IS_DEMO_MODE) return { data: { message: 'Subscription cancelled' } };
    return this.client.post('/subscription/cancel');
  }

  // Notifications endpoints
  async getNotifications(params?: { unreadOnly?: boolean; page?: number; pageSize?: number }) {
    if (IS_DEMO_MODE) return mockApi.getNotifications(params);
    return this.client.get('/notifications', { params });
  }

  async getUnreadNotificationCount() {
    if (IS_DEMO_MODE) return mockApi.getUnreadNotificationCount();
    return this.client.get('/notifications/unread-count');
  }

  async markNotificationAsRead(id: number) {
    if (IS_DEMO_MODE) return mockApi.markNotificationAsRead(id);
    return this.client.put(`/notifications/${id}/read`);
  }

  async markAllNotificationsAsRead() {
    if (IS_DEMO_MODE) return mockApi.markAllNotificationsAsRead();
    return this.client.put('/notifications/mark-all-read');
  }

  async deleteNotification(id: number) {
    if (IS_DEMO_MODE) return mockApi.deleteNotification(id);
    return this.client.delete(`/notifications/${id}`);
  }

  async getNotificationPreferences() {
    if (IS_DEMO_MODE) return mockApi.getNotificationPreferences();
    return this.client.get('/notifications/preferences');
  }

  async updateNotificationPreferences(preferences: any) {
    if (IS_DEMO_MODE) return mockApi.updateNotificationPreferences(preferences);
    return this.client.put('/notifications/preferences', preferences);
  }

  // Team endpoints
  async getTeamMembers(businessId: number) {
    if (IS_DEMO_MODE) return mockApi.getTeamMembers(businessId);
    return this.client.get(`/team/${businessId}/members`);
  }

  async getPendingInvitations(businessId: number) {
    if (IS_DEMO_MODE) return mockApi.getPendingInvitations(businessId);
    return this.client.get(`/team/${businessId}/invitations`);
  }

  async inviteUser(businessId: number, email: string, role: string) {
    if (IS_DEMO_MODE) return mockApi.inviteUser(businessId, email, role);
    return this.client.post(`/team/${businessId}/invite`, { email, role });
  }

  async revokeInvitation(invitationId: number) {
    if (IS_DEMO_MODE) return mockApi.revokeInvitation(invitationId);
    return this.client.delete(`/team/invitations/${invitationId}`);
  }

  async removeTeamMember(businessId: number, userId: number) {
    if (IS_DEMO_MODE) return mockApi.removeTeamMember(businessId, userId);
    return this.client.delete(`/team/${businessId}/members/${userId}`);
  }

  async updateMemberRole(businessId: number, userId: number, role: string) {
    if (IS_DEMO_MODE) return mockApi.updateMemberRole(businessId, userId, role);
    return this.client.put(`/team/${businessId}/members/${userId}/role`, { role });
  }

  // Invitation & Registration endpoints
  async getInvitationDetails(token: string) {
    if (IS_DEMO_MODE) return mockApi.getInvitationDetails(token);
    return this.client.get(`/team/invitation/${token}/details`);
  }

  async acceptInvitationAndRegister(token: string, data: {
    fullName: string;
    password: string;
    phoneNumber?: string;
  }) {
    if (IS_DEMO_MODE) return mockApi.acceptInvitationAndRegister(token, data);
    return this.client.post(`/team/invitation/${token}/accept-and-register`, data);
  }

  // AI endpoints
  async getAISettings() {
    if (IS_DEMO_MODE) return { data: {
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
    }};
    return this.client.get('/ai/settings');
  }

  async updateAISettings(settings: {
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
  }) {
    if (IS_DEMO_MODE) return { data: settings };
    return this.client.put('/ai/settings', settings);
  }

  async generateReviewResponse(reviewId: number) {
    if (IS_DEMO_MODE) return { data: { response: 'Thank you for your wonderful feedback! We truly appreciate your support and look forward to serving you again soon.' } };
    return this.client.post(`/ai/generate-response/${reviewId}`);
  }

  async improveReviewResponse(reviewId: number, originalResponse: string) {
    if (IS_DEMO_MODE) return { data: { response: originalResponse + ' We hope to see you again!' } };
    return this.client.post(`/ai/improve-response/${reviewId}`, { originalResponse });
  }

  async getAnalyticsInsights(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getAnalyticsInsights(businessId, locationId);
    const params = locationId ? `?locationId=${locationId}` : '';
    return this.client.get(`/ai/insights/analytics/${businessId}${params}`);
  }

  async getCompetitorInsights(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getCompetitorInsights(businessId, locationId);
    const params = locationId ? `?locationId=${locationId}` : '';
    return this.client.get(`/ai/insights/competitors/${businessId}${params}`);
  }

  async getReviewSummary(businessId: number, days: number = 30, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getReviewSummary(businessId, days, locationId);
    const params: any = { days };
    if (locationId) params.locationId = locationId;
    return this.client.get(`/ai/insights/review-summary/${businessId}`, { params });
  }

  async getActionableRecommendations(businessId: number, locationId?: number | null) {
    if (IS_DEMO_MODE) return mockApi.getActionableRecommendations(businessId, locationId);
    const params = locationId ? `?locationId=${locationId}` : '';
    return this.client.get(`/ai/insights/recommendations/${businessId}${params}`);
  }

  async generateSocialMediaPost(reviewId: number, platform: string = 'Twitter') {
    if (IS_DEMO_MODE) return { data: { post: '🌟 We love our amazing customers! Thank you for the wonderful review! #CustomerLove #Grateful', platform } };
    return this.client.post(`/ai/generate-social-post/${reviewId}`, null, { params: { platform } });
  }

  // Location endpoints
  async getLocations(organizationId?: number) {
    if (IS_DEMO_MODE) return { data: [
      {
        id: 1,
        name: 'Downtown Store',
        address: '123 Main St',
        city: 'Los Angeles',
        state: 'CA',
        zipCode: '90012',
        country: 'USA',
        phoneNumber: '(555) 123-4567',
        email: 'downtown@acmerestaurants.com',
        isActive: true,
        locationGroup: { id: 1, name: 'California', groupType: 'State' }
      },
      {
        id: 2,
        name: 'Santa Monica Branch',
        address: '456 Ocean Ave',
        city: 'Santa Monica',
        state: 'CA',
        zipCode: '90401',
        country: 'USA',
        phoneNumber: '(555) 234-5678',
        email: 'santamonica@acmerestaurants.com',
        isActive: true,
        locationGroup: { id: 1, name: 'California', groupType: 'State' }
      },
      {
        id: 3,
        name: 'Beverly Hills Location',
        address: '789 Rodeo Dr',
        city: 'Beverly Hills',
        state: 'CA',
        zipCode: '90210',
        country: 'USA',
        phoneNumber: '(555) 345-6789',
        email: 'beverlyhills@acmerestaurants.com',
        isActive: true,
        locationGroup: { id: 1, name: 'California', groupType: 'State' }
      },
      {
        id: 4,
        name: 'San Francisco Store',
        address: '321 Market St',
        city: 'San Francisco',
        state: 'CA',
        zipCode: '94102',
        country: 'USA',
        phoneNumber: '(555) 456-7890',
        email: 'sanfrancisco@acmerestaurants.com',
        isActive: true,
        locationGroup: { id: 1, name: 'California', groupType: 'State' }
      },
      {
        id: 5,
        name: 'Portland Branch',
        address: '555 NW Broadway',
        city: 'Portland',
        state: 'OR',
        zipCode: '97209',
        country: 'USA',
        phoneNumber: '(555) 567-8901',
        email: 'portland@acmerestaurants.com',
        isActive: true,
        locationGroup: { id: 2, name: 'Oregon', groupType: 'State' }
      }
    ] };
    return this.client.get('/locations', { params: { organizationId } });
  }

  async getLocation(id: number) {
    if (IS_DEMO_MODE) {
      const locations = (await this.getLocations()).data;
      return { data: locations.find((l: any) => l.id === id) || null };
    }
    return this.client.get(`/locations/${id}`);
  }

  async createLocation(data: {
    organizationId: number;
    name: string;
    address?: string;
    city?: string;
    state?: string;
    zipCode?: string;
    locationGroupId?: number;
  }) {
    if (IS_DEMO_MODE) {
      console.log('Demo mode: Location created', data);
      return { data: { id: Math.floor(Math.random() * 10000), ...data, isActive: true } };
    }
    return this.client.post('/locations', data);
  }

  async updateLocation(id: number, data: {
    name?: string;
    address?: string;
    city?: string;
    state?: string;
    zipCode?: string;
    phoneNumber?: string;
    email?: string;
  }) {
    if (IS_DEMO_MODE) {
      console.log('Demo mode: Location updated', id, data);
      return { data: { id, ...data } };
    }
    return this.client.put(`/locations/${id}`, data);
  }

  async deleteLocation(id: number) {
    if (IS_DEMO_MODE) {
      console.log('Demo mode: Location deleted', id);
      return { data: { success: true } };
    }
    return this.client.delete(`/locations/${id}`);
  }

  async getLocationGroups(organizationId?: number) {
    if (IS_DEMO_MODE) return { data: [
      {
        id: 1,
        name: 'California',
        groupType: 'State',
        level: 0,
        parentGroupId: null,
        childGroups: [],
        locations: []
      },
      {
        id: 2,
        name: 'Oregon',
        groupType: 'State',
        level: 0,
        parentGroupId: null,
        childGroups: [],
        locations: []
      }
    ] };
    return this.client.get('/locations/groups', { params: { organizationId } });
  }

  async createLocationGroup(data: {
    organizationId: number;
    name: string;
    groupType?: string;
    parentGroupId?: number;
  }) {
    if (IS_DEMO_MODE) return { data: null };
    return this.client.post('/locations/groups', data);
  }

  async compareLocations(locationIds: number[]) {
    if (IS_DEMO_MODE) return { data: [] };
    return this.client.get('/locations/compare', {
      params: { locationIds: locationIds.join(',') }
    });
  }

  async assignUserToLocations(userId: number, locationIds: number[]) {
    if (IS_DEMO_MODE) return { data: null };
    return this.client.post(`/locations/access/user/${userId}/locations`, locationIds);
  }

  async assignUserToLocationGroup(userId: number, groupId: number) {
    if (IS_DEMO_MODE) return { data: null };
    return this.client.post(`/locations/access/user/${userId}/group/${groupId}`);
  }

  async assignUserToAllLocations(userId: number, organizationId: number) {
    if (IS_DEMO_MODE) return { data: null };
    return this.client.post(`/locations/access/user/${userId}/all`, null, {
      params: { organizationId }
    });
  }
}

export const api = new ApiService();
export default api;
