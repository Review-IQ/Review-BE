# ReviewHub API Controllers Documentation

## Overview

ReviewHub backend consists of 11 RESTful API controllers providing complete functionality for review management, customer engagement, analytics, and subscriptions.

---

## 1. AuthController

**Route:** `/api/auth`

### Endpoints

#### POST /api/auth/register
Register a new user after Auth0 signup.

**Request:**
```json
{
  "email": "user@example.com",
  "fullName": "John Doe"
}
```

**Response:** `201 Created`
```json
{
  "id": 1,
  "auth0Id": "auth0|123456",
  "email": "user@example.com",
  "fullName": "John Doe",
  "subscriptionPlan": "Free"
}
```

#### GET /api/auth/me
Get current authenticated user.

**Response:** `200 OK`
- Returns user details
- `404 Not Found` if user not registered (triggers profile completion)
- `403 Forbidden` if account is inactive

#### PUT /api/auth/profile
Update user profile.

**Request:**
```json
{
  "fullName": "John Smith"
}
```

---

## 2. BusinessesController

**Route:** `/api/businesses`

### Endpoints

#### GET /api/businesses
Get all businesses for current user.

#### GET /api/businesses/{id}
Get business by ID.

#### POST /api/businesses
Create a new business.

**Request:**
```json
{
  "name": "My Restaurant",
  "industry": "Food & Beverage",
  "address": "123 Main St",
  "city": "New York",
  "phoneNumber": "+1234567890"
}
```

#### PUT /api/businesses/{id}
Update business details.

#### DELETE /api/businesses/{id}
Delete a business (soft delete with IsActive flag).

---

## 3. ReviewsController

**Route:** `/api/reviews`

### Endpoints

#### GET /api/reviews/{businessId}
Get all reviews for a business with filtering and pagination.

**Query Parameters:**
- `platform` - Filter by platform (Google, Yelp, etc.)
- `sentiment` - Filter by sentiment (Positive, Neutral, Negative)
- `rating` - Filter by rating (1-5)
- `page` - Page number (default: 1)
- `pageSize` - Items per page (default: 20)

#### GET /api/reviews/detail/{id}
Get review details by ID.

#### POST /api/reviews/{id}/reply
Reply to a review.

**Request:**
```json
{
  "responseText": "Thank you for your feedback!"
}
```

#### POST /api/reviews/{id}/flag
Flag a review as important/problematic.

#### POST /api/reviews/{id}/mark-read
Mark a review as read.

---

## 4. IntegrationsController

**Route:** `/api/integrations`

### Endpoints

#### GET /api/integrations/{businessId}
Get all platform connections for a business.

#### POST /api/integrations/connect
Initiate OAuth connection to a platform.

**Request:**
```json
{
  "businessId": 1,
  "platform": "Google"
}
```

**Response:**
```json
{
  "authUrl": "https://accounts.google.com/o/oauth2/auth?...",
  "state": "1:1:abc123"
}
```

#### GET /api/integrations/callback/{platform}
OAuth callback handler (receives authorization code).

#### POST /api/integrations/{id}/disconnect
Disconnect a platform integration.

#### POST /api/integrations/{id}/sync
Manually sync reviews from a platform.

---

## 5. SubscriptionController

**Route:** `/api/subscription`

### Endpoints

#### GET /api/subscription/plans
Get available subscription plans.

**Response:**
```json
{
  "plans": [
    {
      "name": "Free",
      "price": 0,
      "features": ["10 SMS/month", "1 business", "Basic analytics"]
    },
    {
      "name": "Pro",
      "price": 49,
      "priceId": "price_xxx",
      "features": ["500 SMS/month", "5 businesses", "Advanced analytics"]
    }
  ]
}
```

#### POST /api/subscription/create-checkout-session
Create Stripe checkout session.

**Request:**
```json
{
  "plan": "Pro"
}
```

#### POST /api/subscription/create-portal-session
Create Stripe customer portal session.

#### GET /api/subscription/current
Get current user's subscription.

#### POST /api/subscription/cancel
Cancel subscription.

---

## 6. WebhookController

**Route:** `/api/webhook`

### Endpoints

#### POST /api/webhook/stripe
Handle Stripe webhooks (checkout completed, subscription updates).

**Handled Events:**
- `checkout.session.completed` - Save Stripe customer ID
- `customer.subscription.created/updated` - Update subscription plan
- `customer.subscription.deleted` - Revert to Free plan
- `invoice.payment_succeeded` - Log successful payment
- `invoice.payment_failed` - Log failed payment

---

## 7. SmsController

**Route:** `/api/sms`

### Endpoints

#### POST /api/sms/send
Send single SMS.

**Request:**
```json
{
  "businessId": 1,
  "phoneNumber": "+1234567890",
  "message": "Thank you for visiting!"
}
```

#### POST /api/sms/send-bulk
Send bulk SMS with subscription limit checks.

**Request:**
```json
{
  "businessId": 1,
  "phoneNumbers": ["+1234567890", "+0987654321"],
  "message": "Special offer today!"
}
```

**Limits:**
- Free: 10 SMS/month
- Pro: 500 SMS/month
- Enterprise: Unlimited

#### GET /api/sms/messages/{businessId}
Get SMS message history.

#### GET /api/sms/usage/{businessId}
Get SMS usage statistics.

---

## 8. CustomersController

**Route:** `/api/customers`

### Endpoints

#### GET /api/customers/{businessId}
Get all customers for a business with pagination.

#### GET /api/customers/detail/{id}
Get customer details.

#### POST /api/customers
Create a new customer.

**Request:**
```json
{
  "businessId": 1,
  "name": "Jane Smith",
  "email": "jane@example.com",
  "phoneNumber": "+1234567890",
  "notes": "Regular customer"
}
```

#### PUT /api/customers/{id}
Update customer information.

#### DELETE /api/customers/{id}
Delete a customer.

#### POST /api/customers/{id}/record-visit
Record a customer visit (increments total visits, updates last visit).

---

## 9. CampaignsController

**Route:** `/api/campaigns`

### Endpoints

#### GET /api/campaigns/{businessId}
Get all SMS campaigns for a business.

#### GET /api/campaigns/detail/{id}
Get campaign details.

#### POST /api/campaigns
Create and optionally send a campaign.

**Request:**
```json
{
  "businessId": 1,
  "name": "Weekend Special",
  "message": "Come visit us this weekend!",
  "scheduledFor": "2025-10-10T10:00:00Z",
  "recipientPhoneNumbers": ["+1234567890"]
}
```

**Campaign Status:**
- `Draft` - Not scheduled
- `Scheduled` - Scheduled for future
- `Sending` - Currently sending
- `Sent` - Successfully sent
- `Failed` - Send failed

#### POST /api/campaigns/{id}/send
Manually trigger campaign send.

#### PUT /api/campaigns/{id}
Update campaign (only if not sent).

#### DELETE /api/campaigns/{id}
Delete a campaign.

---

## 10. CompetitorsController

**Route:** `/api/competitors`

### Endpoints

#### GET /api/competitors/{businessId}
Get all tracked competitors.

#### GET /api/competitors/detail/{id}
Get competitor details.

#### POST /api/competitors
Add a competitor to track.

**Request:**
```json
{
  "businessId": 1,
  "name": "Competitor Restaurant",
  "platform": "Google",
  "platformBusinessId": "ChIJxxx..."
}
```

#### PUT /api/competitors/{id}
Update competitor information.

#### DELETE /api/competitors/{id}
Stop tracking a competitor.

#### POST /api/competitors/{id}/sync
Sync competitor data (mock implementation - ready for real API).

#### GET /api/competitors/comparison/{businessId}
Get comparison data between business and competitors.

**Response:**
```json
{
  "business": {
    "name": "My Restaurant",
    "averageRating": 4.5,
    "totalReviews": 150
  },
  "competitors": [
    {
      "name": "Competitor 1",
      "platform": "Google",
      "currentRating": 4.2,
      "totalReviews": 200
    }
  ],
  "industryAverage": 4.3,
  "performanceVsIndustry": 0.2
}
```

---

## 11. AnalyticsController

**Route:** `/api/analytics`

### Endpoints

#### GET /api/analytics/overview/{businessId}
Get analytics overview.

**Response:**
```json
{
  "totalReviews": 150,
  "averageRating": 4.5,
  "responseRate": 85.5,
  "sentimentBreakdown": {
    "positive": 120,
    "neutral": 20,
    "negative": 10
  },
  "thisMonthReviews": 25,
  "monthlyChange": 15.5
}
```

#### GET /api/analytics/rating-trend/{businessId}
Get rating trend over time.

**Query Parameters:**
- `months` - Number of months (default: 6)

#### GET /api/analytics/platform-breakdown/{businessId}
Get review breakdown by platform.

#### GET /api/analytics/sentiment-analysis/{businessId}
Get sentiment analysis over time.

**Query Parameters:**
- `days` - Number of days (default: 30)

#### GET /api/analytics/top-keywords/{businessId}
Get most mentioned keywords in reviews.

**Query Parameters:**
- `limit` - Number of keywords (default: 10)

#### GET /api/analytics/response-time/{businessId}
Get response time metrics.

**Response:**
```json
{
  "averageResponseTimeHours": 12.5,
  "medianResponseTimeHours": 8.0,
  "totalResponses": 85,
  "within24Hours": 72,
  "within24HoursPercentage": 84.7
}
```

#### GET /api/analytics/dashboard-summary/{businessId}
Get complete dashboard summary.

---

## Authentication

All endpoints (except webhook) require JWT Bearer token authentication:

```
Authorization: Bearer <auth0_jwt_token>
```

The token is obtained from Auth0 login and contains the user's Auth0 ID in the `sub` claim.

---

## Error Responses

### 400 Bad Request
Invalid input or business logic violation.
```json
{
  "message": "SMS limit exceeded. Your Free plan allows 10 SMS per month."
}
```

### 401 Unauthorized
Missing or invalid authentication token.

### 403 Forbidden
Account inactive or insufficient permissions.
```json
{
  "message": "Account is inactive. Please contact support."
}
```

### 404 Not Found
Resource not found or user not authorized.
```json
{
  "message": "Business not found"
}
```

### 500 Internal Server Error
Server error occurred.
```json
{
  "message": "Failed to process request"
}
```

---

## Database Schema

### Core Entities
- **User** - Auth0 integration, subscription tracking
- **Business** - Multi-tenant business management
- **PlatformConnection** - OAuth token storage
- **Review** - Review content and sentiment
- **SmsMessage** - SMS tracking
- **Competitor** - Competitor tracking
- **Customer** - POS customer management
- **Campaign** - SMS campaign management

### Relationships
- User → Businesses (One-to-Many)
- Business → PlatformConnections (One-to-Many)
- Business → Reviews (One-to-Many)
- Business → SmsMessages (One-to-Many)
- Business → Competitors (One-to-Many)
- Business → Customers (One-to-Many)
- Business → Campaigns (One-to-Many)

---

## Subscription Plans & Limits

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| SMS/month | 10 | 500 | Unlimited |
| Businesses | 1 | 5 | Unlimited |
| Price | $0 | $49/mo | $149/mo |

---

## Development Notes

### Testing the API

1. **Demo Mode**: Set `VITE_DEMO_MODE=true` to test without Auth0
2. **Swagger**: Available at `/swagger` when running locally
3. **Postman Collection**: Import from `docs/ReviewHub.postman_collection.json`

### Adding a New Platform Integration

1. Add platform to `ReviewPlatform` enum in `Core/Enums`
2. Implement OAuth flow in `IntegrationsController`
3. Add platform-specific review fetching logic
4. Update frontend `Integrations.tsx` to include new platform card

### Extending Analytics

The `AnalyticsController` uses LINQ queries on in-memory data. For production:
- Implement caching for frequently accessed data
- Consider pre-computed aggregations for large datasets
- Use database views for complex queries

---

Last Updated: October 2, 2025
