# ReviewHub Testing Guide

Complete guide for testing ReviewHub locally and in production.

---

## Quick Test (Demo Mode - 30 seconds)

The fastest way to test the entire application:

### 1. Setup Demo Mode

```bash
cd C:\myStuff\ReviewHub\client
copy .env.demo .env
npm install
npm run dev
```

### 2. Test in Browser

1. Open http://localhost:5173
2. You're automatically logged in as "Demo User"
3. All features work with mock data (no backend needed)

**Test Checklist:**
- ✅ Dashboard loads with metrics
- ✅ Reviews page shows sample reviews
- ✅ Can reply to reviews
- ✅ Can filter and search reviews
- ✅ Integrations page shows platforms
- ✅ Analytics displays charts
- ✅ POS Automation shows customers and campaigns
- ✅ Competitors page displays comparison
- ✅ Settings page accessible

---

## Full Stack Testing (With Database - 15 minutes)

Test the complete application with real backend and database.

### Prerequisites

- .NET 9.0 SDK installed
- SQL Server or LocalDB running
- Node.js 18+ installed

### 1. Backend Setup

```bash
# Navigate to API project
cd C:\myStuff\ReviewHub\src\ReviewHub.API

# Update connection string in appsettings.json
# "Server=(localdb)\\mssqllocaldb;Database=ReviewHubDb;Trusted_Connection=True;MultipleActiveResultSets=true"

# Run migrations to create database
dotnet ef database update

# Start backend
dotnet run
```

**Verify backend is running:** http://localhost:5000/swagger

### 2. Auth0 Setup (Optional for full auth)

If testing authentication:

1. Go to https://manage.auth0.com/
2. Create a Single Page Application
3. Note Domain and Client ID
4. Configure:
   - Allowed Callback URLs: `http://localhost:5173/callback`
   - Allowed Logout URLs: `http://localhost:5173`
   - Allowed Web Origins: `http://localhost:5173`
5. Create API with identifier: `https://api.reviewhub.com`

### 3. Frontend Setup

```bash
cd C:\myStuff\ReviewHub\client

# Create .env file
# Copy content from .env.example and update:
# VITE_DEMO_MODE=false
# VITE_API_URL=http://localhost:5000/api
# VITE_AUTH0_DOMAIN=your-domain.auth0.com
# VITE_AUTH0_CLIENT_ID=your-client-id
# VITE_AUTH0_AUDIENCE=https://api.reviewhub.com

npm install
npm run dev
```

### 4. Test Authentication Flow

1. Open http://localhost:5173
2. Click "Sign In" → redirects to Auth0
3. Register new account or login
4. After Auth0 login, you'll be prompted to complete profile
5. Fill in Full Name → saves to database
6. Redirected to Dashboard

**Verify:**
- User created in database (check Users table)
- JWT token in browser localStorage
- API calls include Authorization header

---

## API Testing

### Using Swagger UI

1. Run backend: `dotnet run` in ReviewHub.API folder
2. Open http://localhost:5000/swagger
3. Test endpoints directly

### Using Postman

Import the collection from `docs/ReviewHub.postman_collection.json` (create if needed)

**Test Sequence:**

1. **Register User** - POST `/api/auth/register`
```json
{
  "email": "test@example.com",
  "fullName": "Test User"
}
```

2. **Create Business** - POST `/api/businesses`
```json
{
  "name": "Test Restaurant",
  "industry": "Food & Beverage",
  "city": "New York"
}
```

3. **Create Customer** - POST `/api/customers`
```json
{
  "businessId": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "phoneNumber": "+1234567890"
}
```

4. **Send SMS** - POST `/api/sms/send`
```json
{
  "businessId": 1,
  "phoneNumber": "+1234567890",
  "message": "Thank you for visiting!"
}
```

---

## Feature-by-Feature Testing

### 1. Business Management

**Test Cases:**
- ✅ Create new business
- ✅ Update business details
- ✅ List all businesses for user
- ✅ Delete business (soft delete - check IsActive)

**Test Data:**
```json
{
  "name": "Main Street Cafe",
  "industry": "Food & Beverage",
  "description": "Cozy cafe in downtown",
  "website": "https://mainstreetcafe.com",
  "phoneNumber": "+12025551234",
  "address": "123 Main St",
  "city": "Washington",
  "state": "DC",
  "zipCode": "20001"
}
```

### 2. Platform Integrations

**Mock OAuth Flow:**
1. Go to Integrations page
2. Click "Connect" on Google
3. Redirected to mock OAuth (or real if configured)
4. After callback, platform shows as "Connected"
5. Click "Sync" to fetch reviews (mock data)

**Verify:**
- PlatformConnection created in database
- AccessToken and RefreshToken stored (encrypted in production)
- Platform shows green "Connected" status

### 3. Reviews Management

**Test Cases:**
- ✅ View all reviews (paginated)
- ✅ Filter by platform (Google, Yelp, Facebook)
- ✅ Filter by sentiment (Positive, Neutral, Negative)
- ✅ Filter by rating (1-5 stars)
- ✅ Reply to review
- ✅ Mark as read/unread
- ✅ Flag important reviews
- ✅ Search reviews by text

**Test Reply:**
1. Click "Reply" on any review
2. Use AI suggested response or write custom
3. Submit
4. Verify ResponseText and ResponseDate saved

### 4. Customer & Campaign Management

**Create Customer:**
```json
{
  "businessId": 1,
  "name": "Sarah Johnson",
  "email": "sarah@example.com",
  "phoneNumber": "+12025555678",
  "notes": "VIP customer - allergic to peanuts"
}
```

**Create Campaign:**
```json
{
  "businessId": 1,
  "name": "Weekend Special",
  "message": "Come visit us this weekend for 20% off!",
  "scheduledFor": "2025-10-15T10:00:00Z",
  "recipientPhoneNumbers": ["+12025555678"]
}
```

**Test:**
- Campaign status changes: Draft → Scheduled → Sending → Sent
- SMS messages logged in SmsMessages table
- Usage tracking respects subscription limits

### 5. Analytics

**Test Endpoints:**
- GET `/api/analytics/overview/{businessId}` - Overall metrics
- GET `/api/analytics/rating-trend/{businessId}` - Rating over time
- GET `/api/analytics/platform-breakdown/{businessId}` - Reviews by platform
- GET `/api/analytics/sentiment-analysis/{businessId}` - Sentiment trends
- GET `/api/analytics/top-keywords/{businessId}` - Most mentioned words
- GET `/api/analytics/response-time/{businessId}` - Response speed metrics
- GET `/api/analytics/dashboard-summary/{businessId}` - Complete dashboard data

**Verify Charts Display:**
- Line chart for rating trends
- Bar chart for platform breakdown
- Pie chart for sentiment distribution
- Keyword cloud (if implemented)

### 6. Competitor Tracking

**Add Competitor:**
```json
{
  "businessId": 1,
  "name": "Rival Restaurant",
  "platform": "Google",
  "platformBusinessId": "ChIJxxxxxxxxxxxxx"
}
```

**Test Sync:**
1. Click "Sync Data" on competitor
2. Mock data updates: CurrentRating, TotalReviews, LastCheckedAt
3. View comparison: business vs competitors vs industry average

### 7. Subscription & Billing

**Test Stripe Integration (requires Stripe test keys):**

1. **View Plans:**
   - GET `/api/subscription/plans`
   - Verify: Free (0), Pro ($49), Enterprise ($149)

2. **Create Checkout Session:**
   - POST `/api/subscription/create-checkout-session`
   - Get Stripe checkout URL
   - Complete payment with test card: `4242 4242 4242 4242`

3. **Webhook Events:**
   - Simulate webhook: `checkout.session.completed`
   - Verify: User.StripeCustomerId updated
   - Verify: User.SubscriptionPlan updated to "Pro"

4. **Customer Portal:**
   - POST `/api/subscription/create-portal-session`
   - User can manage subscription, view invoices, update payment method

**Test Cards:**
- Success: `4242 4242 4242 4242`
- Decline: `4000 0000 0000 0002`
- 3D Secure: `4000 0025 0000 3155`

### 8. SMS Functionality

**Test Twilio Integration (requires Twilio credentials):**

1. **Single SMS:**
```json
{
  "businessId": 1,
  "phoneNumber": "+1234567890",
  "message": "Thank you for your visit!"
}
```

2. **Bulk SMS:**
```json
{
  "businessId": 1,
  "phoneNumbers": ["+1234567890", "+0987654321"],
  "message": "Special offer this week!"
}
```

3. **Subscription Limits:**
   - Free plan: Try sending 11th SMS (should fail)
   - Pro plan: Can send up to 500/month
   - Enterprise: Unlimited

4. **Usage Tracking:**
   - GET `/api/sms/usage/{businessId}`
   - Verify: sentThisMonth, monthlyLimit, remaining

---

## Database Testing

### Verify Database Schema

```sql
-- Check all tables exist
SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME;

-- Expected tables:
-- Businesses
-- Campaigns
-- Competitors
-- Customers
-- PlatformConnections
-- Reviews
-- SmsMessages
-- Users
-- __EFMigrationsHistory

-- Check user data
SELECT * FROM Users;

-- Check relationships
SELECT u.FullName, b.Name as BusinessName
FROM Users u
JOIN Businesses b ON u.Id = b.UserId;
```

### Test Data Integrity

1. **Cascade Deletes:**
   - Delete a business
   - Verify: Related PlatformConnections, Reviews, Customers deleted

2. **Unique Constraints:**
   - Try creating duplicate user (same Auth0Id) → should fail
   - Try creating duplicate customer (same phone) → should fail

3. **Indexes:**
   - Check query performance on Reviews table
   - Verify indexes on: Platform, Sentiment, Rating, ReviewDate

---

## Performance Testing

### Load Testing

Use tools like Apache Bench or k6:

```bash
# Test reviews endpoint
ab -n 1000 -c 10 http://localhost:5000/api/reviews?businessId=1

# Test analytics
ab -n 500 -c 5 http://localhost:5000/api/analytics/overview/1
```

**Performance Benchmarks:**
- Reviews list (100 items): < 200ms
- Analytics overview: < 500ms
- Dashboard summary: < 1s
- SMS send: < 3s (Twilio API latency)

### Database Optimization

```sql
-- Check query execution plans
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

SELECT * FROM Reviews WHERE BusinessId = 1 AND Sentiment = 'Positive';

-- Verify indexes are used
EXEC sp_helpindex 'Reviews';
```

---

## Security Testing

### Authentication

1. **Without Token:**
   - Try accessing `/api/businesses` without Authorization header
   - Expected: 401 Unauthorized

2. **Invalid Token:**
   - Use expired or malformed JWT
   - Expected: 401 Unauthorized

3. **Wrong User:**
   - User A tries to access User B's business
   - Expected: 404 Not Found (user not authorized)

### Authorization

1. **Cross-tenant Access:**
   - Create 2 users
   - User 1 tries to GET `/api/businesses/{user2BusinessId}`
   - Expected: 404 Not Found

2. **Inactive Account:**
   - Set User.IsActive = false
   - Try to login
   - Expected: 403 Forbidden

### SQL Injection

Test with malicious input:
```
businessName: "'; DROP TABLE Users; --"
```
Expected: Parameterized queries prevent injection

### XSS Prevention

Test review text with script:
```html
<script>alert('XSS')</script>
```
Expected: Frontend sanitizes HTML before display

---

## Error Handling Testing

### Test Error Responses

1. **400 Bad Request:**
   - Send SMS beyond limit
   - Create business with missing required fields

2. **404 Not Found:**
   - GET `/api/businesses/999999` (non-existent ID)
   - GET `/api/reviews/detail/999999`

3. **403 Forbidden:**
   - Inactive user tries to access system
   - User tries to access another user's resource

4. **500 Internal Server Error:**
   - Simulate database connection failure
   - Check error is logged, generic message returned

---

## Mobile Responsiveness Testing

### Devices to Test

1. **Desktop:** 1920x1080, 1366x768
2. **Tablet:** iPad (768x1024), iPad Pro (1024x1366)
3. **Mobile:** iPhone 14 (390x844), Samsung Galaxy (360x800)

### Test Checklist

- ✅ Navigation menu collapses to hamburger
- ✅ Tables become scrollable horizontally
- ✅ Cards stack vertically on mobile
- ✅ Forms adapt to smaller screens
- ✅ Charts remain readable
- ✅ Touch targets minimum 44x44px
- ✅ Text remains legible (min 16px)

### Browser DevTools

```
Chrome DevTools:
1. F12 → Toggle device toolbar (Ctrl+Shift+M)
2. Test various devices
3. Check responsive breakpoints: 640px, 768px, 1024px, 1280px
```

---

## Automated Testing (Future)

### Unit Tests

```bash
# Backend tests
cd src/ReviewHub.API.Tests
dotnet test

# Frontend tests
cd client
npm run test
```

### Integration Tests

```csharp
[Fact]
public async Task CreateBusiness_ValidData_ReturnsCreated()
{
    // Arrange
    var client = _factory.CreateClient();
    var business = new { name = "Test", industry = "Food" };

    // Act
    var response = await client.PostAsJsonAsync("/api/businesses", business);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

### E2E Tests (Playwright)

```typescript
test('user can create business', async ({ page }) => {
  await page.goto('http://localhost:5173');
  await page.click('text=Add Business');
  await page.fill('[name="name"]', 'Test Restaurant');
  await page.click('text=Save');
  await expect(page.locator('text=Test Restaurant')).toBeVisible();
});
```

---

## Deployment Testing

### Pre-Deployment Checklist

- ✅ All tests pass
- ✅ Build completes without errors
- ✅ Migrations applied to staging database
- ✅ Environment variables configured
- ✅ SSL certificates valid
- ✅ CORS configured for production domain
- ✅ Stripe in live mode (not test)
- ✅ Auth0 configured for production URLs
- ✅ Twilio using production credentials

### Staging Environment

1. Deploy to Azure App Service (staging slot)
2. Run smoke tests on staging
3. Verify all integrations work
4. Test with production-like data volume
5. Check performance under load
6. Swap to production slot

### Production Smoke Tests

After deployment:
1. ✅ Homepage loads
2. ✅ Login works
3. ✅ API health check: GET `/health`
4. ✅ Database connectivity
5. ✅ Stripe webhooks receiving events
6. ✅ SMS sending functional

---

## Monitoring & Logging

### Application Insights

```csharp
// Check telemetry is being sent
TelemetryClient.TrackEvent("BusinessCreated", new { businessId = 1 });
TelemetryClient.TrackException(ex);
```

### Log Levels

- **Error:** Failed operations, exceptions
- **Warning:** SMS limit exceeded, payment failed
- **Information:** User registered, review replied
- **Debug:** Detailed request/response data

### Alerts to Configure

1. **Error Rate** > 5% in 5 minutes
2. **Response Time** > 2s average
3. **Failed Payments** any occurrence
4. **SMS Sending Failures** > 3 in 1 hour
5. **Database Connection** failures

---

## Common Issues & Solutions

### Issue: "User not found in system"
**Solution:** User registered in Auth0 but not in ReviewHub database. Complete profile registration.

### Issue: "SMS limit exceeded"
**Solution:** User reached monthly SMS limit for their plan. Upgrade subscription.

### Issue: "Platform not connected"
**Solution:** OAuth flow not completed. Re-initiate connection from Integrations page.

### Issue: "Reviews not syncing"
**Solution:**
1. Check platform connection is active
2. Verify OAuth token not expired
3. Check platform API status
4. Review error logs

### Issue: "Payment failed"
**Solution:**
1. Check Stripe webhook is receiving events
2. Verify webhook secret matches
3. Check customer has valid payment method
4. Review Stripe dashboard for detailed error

---

## Test Data Cleanup

After testing, clean up test data:

```sql
-- Development only - DO NOT run in production!
DELETE FROM SmsMessages WHERE BusinessId IN (SELECT Id FROM Businesses WHERE Name LIKE '%Test%');
DELETE FROM Reviews WHERE BusinessId IN (SELECT Id FROM Businesses WHERE Name LIKE '%Test%');
DELETE FROM Customers WHERE BusinessId IN (SELECT Id FROM Businesses WHERE Name LIKE '%Test%');
DELETE FROM Campaigns WHERE BusinessId IN (SELECT Id FROM Businesses WHERE Name LIKE '%Test%');
DELETE FROM PlatformConnections WHERE BusinessId IN (SELECT Id FROM Businesses WHERE Name LIKE '%Test%');
DELETE FROM Competitors WHERE BusinessId IN (SELECT Id FROM Businesses WHERE Name LIKE '%Test%');
DELETE FROM Businesses WHERE Name LIKE '%Test%';
DELETE FROM Users WHERE Email LIKE '%@test.com';
```

---

## Support & Documentation

- **API Docs:** `/docs/API_CONTROLLERS.md`
- **Getting Started:** `/GETTING_STARTED.md`
- **Demo Mode:** `/docs/DEMO_MODE.md`
- **Deployment:** `/docs/DEPLOYMENT.md`
- **Implementation Status:** `/docs/IMPLEMENTATION_STATUS.md`

---

Last Updated: October 2, 2025
