# ReviewHub API Documentation

Base URL: `http://localhost:5000/api` (Development)

All authenticated endpoints require a Bearer token in the Authorization header:
```
Authorization: Bearer {your_jwt_token}
```

## Authentication

### Authentication Flow

1. **User signs up/logs in via Auth0** - User authenticates with Auth0 and receives JWT token
2. **First login** - Frontend calls `GET /api/auth/me` to check if user exists in our system
   - If 404 → Redirect to registration completion page
   - If 403 → Account is inactive, show error
   - If 200 → User exists and is active, proceed to dashboard
3. **Registration** - New users call `POST /api/auth/register` to create account in our system
4. **Subsequent logins** - `GET /api/auth/me` returns user data if active

### Register User
Complete user registration in our system after Auth0 signup.

**Endpoint:** `POST /api/auth/register`

**Authentication:** Required (Auth0 JWT)

**Request Body:**
```json
{
  "email": "user@example.com",
  "fullName": "John Doe",
  "companyName": "Acme Inc.",
  "phoneNumber": "+1 (555) 123-4567"
}
```

**Response:** `200 OK`
```json
{
  "user": {
    "id": 1,
    "email": "user@example.com",
    "fullName": "John Doe",
    "companyName": "Acme Inc.",
    "phoneNumber": "+1 (555) 123-4567",
    "subscriptionPlan": "Free",
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "message": "Registration successful"
}
```

**Note:** If user already exists, returns existing user with message "User already registered"

### Get Current User
Retrieve the authenticated user's profile and verify account is active.

**Endpoint:** `GET /api/auth/me`

**Authentication:** Required

**Response:** `200 OK` - User exists and is active
```json
{
  "id": 1,
  "email": "user@example.com",
  "fullName": "John Doe",
  "companyName": "Acme Inc.",
  "phoneNumber": "+1 (555) 123-4567",
  "subscriptionPlan": "Pro",
  "subscriptionExpiresAt": "2024-12-31T23:59:59Z",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Response:** `404 Not Found` - User needs to complete registration
```json
{
  "message": "User not found in system",
  "needsRegistration": true,
  "auth0Id": "auth0|123456789",
  "email": "user@example.com"
}
```

**Response:** `403 Forbidden` - Account is inactive
```json
{
  "message": "Account is inactive. Please contact support."
}
```

### Update Profile
Update user profile information.

**Endpoint:** `PUT /api/auth/profile`

**Authentication:** Required

**Request Body:**
```json
{
  "fullName": "John Smith",
  "email": "john.smith@example.com"
}
```

**Response:** `200 OK`

---

## Businesses

### List Businesses
Get all businesses for the authenticated user.

**Endpoint:** `GET /api/businesses`

**Authentication:** Required

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "name": "Main Street Cafe",
    "industry": "Restaurant",
    "city": "New York",
    "state": "NY",
    "platformConnectionsCount": 3,
    "reviewsCount": 247,
    "avgRating": 4.5,
    "createdAt": "2024-01-01T00:00:00Z"
  }
]
```

### Get Business
Get a specific business with detailed stats.

**Endpoint:** `GET /api/businesses/{id}`

**Authentication:** Required

**Response:** `200 OK`
```json
{
  "id": 1,
  "name": "Main Street Cafe",
  "industry": "Restaurant",
  "description": "A cozy cafe in downtown",
  "website": "https://mainstreetcafe.com",
  "phoneNumber": "+1234567890",
  "address": "123 Main St",
  "city": "New York",
  "state": "NY",
  "zipCode": "10001",
  "country": "USA",
  "logoUrl": "https://example.com/logo.png",
  "platformConnections": [
    {
      "id": 1,
      "platform": "Google",
      "connectedAt": "2024-01-15T10:00:00Z",
      "lastSyncedAt": "2024-01-20T14:30:00Z"
    }
  ],
  "stats": {
    "totalReviews": 247,
    "avgRating": 4.5,
    "unreadReviews": 12,
    "flaggedReviews": 3
  }
}
```

### Create Business
Create a new business.

**Endpoint:** `POST /api/businesses`

**Authentication:** Required

**Request Body:**
```json
{
  "name": "Downtown Restaurant",
  "industry": "Restaurant",
  "description": "Fine dining experience",
  "website": "https://downtown-restaurant.com",
  "phoneNumber": "+1987654321",
  "address": "456 Park Ave",
  "city": "New York",
  "state": "NY",
  "zipCode": "10002",
  "country": "USA"
}
```

**Response:** `201 Created`
```json
{
  "id": 2,
  "name": "Downtown Restaurant",
  "industry": "Restaurant",
  "createdAt": "2024-01-20T15:00:00Z"
}
```

### Update Business
Update business details.

**Endpoint:** `PUT /api/businesses/{id}`

**Authentication:** Required

**Request Body:** (All fields optional)
```json
{
  "name": "Downtown Fine Dining",
  "phoneNumber": "+1234567890"
}
```

**Response:** `200 OK`

### Delete Business
Soft delete a business (sets IsActive = false).

**Endpoint:** `DELETE /api/businesses/{id}`

**Authentication:** Required

**Response:** `204 No Content`

---

## Reviews

### List Reviews
Get reviews with filtering and pagination.

**Endpoint:** `GET /api/reviews`

**Authentication:** Required

**Query Parameters:**
- `businessId` (int, optional) - Filter by business
- `platform` (string, optional) - Filter by platform (Google, Yelp, Facebook, etc.)
- `sentiment` (string, optional) - Filter by sentiment (Positive, Neutral, Negative)
- `rating` (int, optional) - Filter by rating (1-5)
- `isRead` (bool, optional) - Filter by read status
- `isFlagged` (bool, optional) - Filter by flagged status
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 20) - Items per page

**Example:** `GET /api/reviews?platform=Google&rating=5&page=1&pageSize=10`

**Response:** `200 OK`
```json
{
  "data": [
    {
      "id": 1,
      "businessId": 1,
      "businessName": "Main Street Cafe",
      "platform": "Google",
      "platformReviewId": "google-abc123",
      "reviewerName": "Jane Smith",
      "reviewerEmail": "jane@example.com",
      "rating": 5,
      "reviewText": "Amazing food and service!",
      "reviewDate": "2024-01-18T12:00:00Z",
      "responseText": "Thank you for your kind words!",
      "responseDate": "2024-01-18T14:00:00Z",
      "sentiment": "Positive",
      "sentimentScore": 0.95,
      "aiSuggestedResponse": "We're thrilled to hear you enjoyed...",
      "isRead": true,
      "isFlagged": false,
      "location": "New York, NY"
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 247,
    "totalPages": 25
  }
}
```

### Get Review
Get a specific review by ID.

**Endpoint:** `GET /api/reviews/{id}`

**Authentication:** Required

**Response:** `200 OK` (same structure as list item)

### Reply to Review
Post a response to a review.

**Endpoint:** `POST /api/reviews/{id}/reply`

**Authentication:** Required

**Request Body:**
```json
{
  "responseText": "Thank you for your feedback! We appreciate your business."
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "responseText": "Thank you for your feedback!",
  "responseDate": "2024-01-20T16:00:00Z"
}
```

### Mark as Read/Unread
Toggle read status of a review.

**Endpoint:** `PATCH /api/reviews/{id}/read`

**Authentication:** Required

**Request Body:**
```json
{
  "isRead": true
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "isRead": true
}
```

### Flag/Unflag Review
Toggle flag status for important reviews.

**Endpoint:** `PATCH /api/reviews/{id}/flag`

**Authentication:** Required

**Request Body:**
```json
{
  "isFlagged": true
}
```

**Response:** `200 OK`
```json
{
  "id": 1,
  "isFlagged": true
}
```

---

## Integrations

### Get Available Platforms
List all supported review platforms.

**Endpoint:** `GET /api/integrations/platforms`

**Authentication:** Not Required

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "name": "Google",
    "displayName": "Google Business Profile",
    "description": "Connect your Google Business Profile",
    "supportsOAuth": true,
    "isComingSoon": false
  },
  {
    "id": 2,
    "name": "Yelp",
    "displayName": "Yelp",
    "description": "Sync Yelp reviews",
    "supportsOAuth": true,
    "isComingSoon": false
  }
]
```

### Get Business Connections
Get all platform connections for a business.

**Endpoint:** `GET /api/integrations/business/{businessId}`

**Authentication:** Required

**Response:** `200 OK`
```json
[
  {
    "id": 1,
    "platform": "Google",
    "platformBusinessId": "ChIJN1t_tDeuEmsRUsoyG83frY4",
    "platformBusinessName": "Main Street Cafe",
    "connectedAt": "2024-01-15T10:00:00Z",
    "lastSyncedAt": "2024-01-20T14:30:00Z",
    "isActive": true,
    "autoSync": true,
    "syncIntervalMinutes": 60
  }
]
```

### Initiate Platform Connection
Start OAuth flow for connecting a platform.

**Endpoint:** `POST /api/integrations/connect/{platform}`

**Authentication:** Required

**Path Parameters:**
- `platform` - Platform name (google, yelp, facebook)

**Request Body:**
```json
{
  "businessId": 1
}
```

**Response:** `200 OK`
```json
{
  "authUrl": "https://accounts.google.com/o/oauth2/v2/auth?client_id=..."
}
```

**Usage:** Redirect user to `authUrl` to complete OAuth.

### OAuth Callback
Handle OAuth callback from platform.

**Endpoint:** `GET /api/integrations/callback/{platform}`

**Authentication:** Not Required (uses state parameter)

**Query Parameters:**
- `code` - Authorization code from OAuth provider
- `state` - State parameter (contains userId:businessId)
- `error` (optional) - Error from OAuth provider

**Response:** `302 Redirect`

Redirects to frontend with success/error status:
- Success: `{FRONTEND_URL}/integrations?success=true`
- Error: `{FRONTEND_URL}/integrations?error=access_denied`

### Disconnect Platform
Remove platform connection.

**Endpoint:** `DELETE /api/integrations/{connectionId}`

**Authentication:** Required

**Response:** `204 No Content`

### Sync Reviews
Manually trigger review sync for a connection.

**Endpoint:** `POST /api/integrations/{connectionId}/sync`

**Authentication:** Required

**Response:** `200 OK`
```json
{
  "success": true,
  "reviewsSynced": 15,
  "lastSyncedAt": "2024-01-20T16:30:00Z"
}
```

---

## Error Responses

### 400 Bad Request
```json
{
  "message": "Invalid request data",
  "errors": {
    "email": ["Email is required"]
  }
}
```

### 401 Unauthorized
```json
{
  "message": "Unauthorized access"
}
```

### 404 Not Found
```json
{
  "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
  "message": "An error occurred processing your request"
}
```

---

## Rate Limiting

- **Authenticated requests:** 100 requests per minute
- **Public endpoints:** 20 requests per minute

Rate limit headers:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1642684800
```

---

## Webhooks (Coming Soon)

### Stripe Webhook
**Endpoint:** `POST /api/webhooks/stripe`

Handle subscription events from Stripe.

### Platform Webhooks
**Endpoint:** `POST /api/webhooks/{platform}`

Receive real-time review notifications from platforms.

---

## Testing

Use Swagger UI for interactive API testing:
- Development: `http://localhost:5000/swagger`
- Staging: `https://api-staging.reviewhub.com/swagger`

## SDKs & Libraries

Official SDKs coming soon:
- JavaScript/TypeScript
- Python
- Ruby
- PHP
