# OAuth Integration Setup Guide

This guide explains how to set up OAuth integrations for Google Business Profile, Yelp, and Facebook to enable review syncing in ReviewHub.

---

## 🔐 Google Business Profile OAuth Setup

### 1. Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the **Google My Business API**:
   - Go to "APIs & Services" → "Library"
   - Search for "My Business Business Information API"
   - Click "Enable"

### 2. Create OAuth 2.0 Credentials

1. Go to "APIs & Services" → "Credentials"
2. Click "Create Credentials" → "OAuth client ID"
3. Choose "Web application"
4. Configure:
   - **Name**: ReviewHub Google Integration
   - **Authorized JavaScript origins**: `http://localhost:5173` (add production URL later)
   - **Authorized redirect URIs**:
     - `http://localhost:7250/api/integrations/callback/google`
     - `https://localhost:7251/api/integrations/callback/google`
     - Add your production API URL when deploying
5. Click "Create"
6. Copy the **Client ID** and **Client Secret**

### 3. Configure OAuth Consent Screen

1. Go to "OAuth consent screen"
2. Choose "External" (or "Internal" for Google Workspace)
3. Fill in required fields:
   - App name: ReviewHub
   - User support email: your email
   - Developer contact: your email
4. Add scopes:
   - `https://www.googleapis.com/auth/business.manage`
   - `https://www.googleapis.com/auth/plus.business.manage`
5. Save and continue

### 4. Add to Configuration

Update `src/ReviewHub.API/appsettings.json`:

```json
{
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
    "RedirectUri": "http://localhost:7250/api/integrations/callback/google"
  }
}
```

---

## 🔍 Yelp Fusion API OAuth Setup

### 1. Create Yelp App

1. Go to [Yelp Developers](https://www.yelp.com/developers)
2. Sign in or create an account
3. Click "Create App"
4. Fill in the form:
   - **App Name**: ReviewHub
   - **Industry**: Technology
   - **Contact Email**: your email
   - **Description**: Review management platform

### 2. Get API Credentials

1. After creating the app, you'll see your credentials
2. Copy the **Client ID** and **API Key** (Client Secret)

### 3. Configure OAuth Settings

1. In your Yelp app settings, add redirect URIs:
   - `http://localhost:7250/api/integrations/callback/yelp`
   - `https://localhost:7251/api/integrations/callback/yelp`
   - Add your production API URL when deploying

### 4. Add to Configuration

Update `src/ReviewHub.API/appsettings.json`:

```json
{
  "Yelp": {
    "ClientId": "YOUR_YELP_CLIENT_ID",
    "ClientSecret": "YOUR_YELP_API_KEY",
    "RedirectUri": "http://localhost:7250/api/integrations/callback/yelp"
  }
}
```

### Note on Yelp Reviews

Yelp's API has some limitations:
- Reviews require business verification
- Only returns up to 3 reviews per business
- For full review access, you need to apply for Yelp's review API access

---

## 📘 Facebook OAuth Setup

### 1. Create Facebook App

1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Click "My Apps" → "Create App"
3. Choose "Business" as the app type
4. Fill in app details:
   - **App Name**: ReviewHub
   - **App Contact Email**: your email
   - **Business Account**: (optional)

### 2. Configure Facebook Login

1. In your app dashboard, click "Add Product"
2. Find "Facebook Login" and click "Set Up"
3. Choose "Web" platform
4. Enter Site URL: `http://localhost:5173`
5. Save settings

### 3. Configure OAuth Settings

1. Go to "Facebook Login" → "Settings"
2. Add Valid OAuth Redirect URIs:
   - `http://localhost:7250/api/integrations/callback/facebook`
   - `https://localhost:7251/api/integrations/callback/facebook`
   - Add your production API URL when deploying
3. Save changes

### 4. Get App Credentials

1. Go to "Settings" → "Basic"
2. Copy the **App ID** and **App Secret**

### 5. Request Permissions

For accessing page reviews, you need to request these permissions:
- `pages_show_list`
- `pages_read_engagement`
- `pages_manage_metadata`

These permissions require app review by Facebook before going live.

### 6. Add to Configuration

Update `src/ReviewHub.API/appsettings.json`:

```json
{
  "Facebook": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET",
    "RedirectUri": "http://localhost:7250/api/integrations/callback/facebook"
  }
}
```

---

## 🔧 Complete Configuration Example

Here's how your `appsettings.json` should look:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ReviewHubDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com",
    "Audience": "https://api.reviewhub.com",
    "ClientId": "your-auth0-client-id"
  },
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
    "RedirectUri": "http://localhost:7250/api/integrations/callback/google"
  },
  "Yelp": {
    "ClientId": "YOUR_YELP_CLIENT_ID",
    "ClientSecret": "YOUR_YELP_API_KEY",
    "RedirectUri": "http://localhost:7250/api/integrations/callback/yelp"
  },
  "Facebook": {
    "AppId": "YOUR_FACEBOOK_APP_ID",
    "AppSecret": "YOUR_FACEBOOK_APP_SECRET",
    "RedirectUri": "http://localhost:7250/api/integrations/callback/facebook"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_...",
    "PriceIds": {
      "Free": "price_...",
      "Pro": "price_...",
      "Enterprise": "price_..."
    }
  },
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "your-auth-token",
    "PhoneNumber": "+1234567890"
  },
  "App": {
    "FrontendUrl": "http://localhost:5173"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

---

## 🔄 OAuth Flow Overview

### How It Works

1. **User Initiates Connection**:
   - User clicks "Connect" button for a platform in the Integrations page
   - Frontend calls `POST /api/integrations/connect/{platform}` with `businessId`

2. **Backend Generates Auth URL**:
   - API calls the appropriate service (Google/Yelp/Facebook)
   - Service generates OAuth authorization URL with scopes and state
   - API returns the URL to frontend

3. **User Authorizes**:
   - Frontend redirects user to the OAuth URL
   - User logs in to the platform and grants permissions
   - Platform redirects back to the callback URL with authorization code

4. **Backend Exchanges Code for Tokens**:
   - Callback endpoint receives the code and state
   - API calls the service to exchange code for access/refresh tokens
   - Tokens are saved to database in `PlatformConnections` table

5. **Review Syncing**:
   - User can manually trigger sync via "Sync Now" button
   - API calls `POST /api/integrations/{connectionId}/sync`
   - Service fetches reviews from platform API using stored tokens
   - New reviews are saved to database

### API Endpoints

```
POST   /api/integrations/connect/{platform}           - Initiate OAuth flow
GET    /api/integrations/callback/{platform}          - OAuth callback handler
GET    /api/integrations/business/{businessId}        - Get all connections
POST   /api/integrations/{connectionId}/sync          - Sync reviews
DELETE /api/integrations/{connectionId}               - Disconnect platform
```

---

## 🧪 Testing OAuth Locally

### 1. Start the Backend

```bash
cd src/ReviewHub.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:7250`
- HTTPS: `https://localhost:7251`

### 2. Start the Frontend

```bash
cd client
npm run dev
```

The frontend will be available at `http://localhost:5173`

### 3. Test OAuth Flow

1. Log in to the application
2. Create a business
3. Go to Integrations page
4. Click "Connect" on Google/Yelp/Facebook
5. You'll be redirected to the platform's login page
6. Grant permissions
7. You'll be redirected back to the app
8. The platform should now show as "Connected"
9. Click "Sync Now" to fetch reviews

---

## 🚀 Production Deployment

### Update Redirect URIs

When deploying to production, update the redirect URIs in each platform:

**Google Cloud Console:**
- Add: `https://api.yourdomain.com/api/integrations/callback/google`

**Yelp App Settings:**
- Add: `https://api.yourdomain.com/api/integrations/callback/yelp`

**Facebook App Settings:**
- Add: `https://api.yourdomain.com/api/integrations/callback/facebook`

### Update Configuration

Update `appsettings.Production.json`:

```json
{
  "Google": {
    "RedirectUri": "https://api.yourdomain.com/api/integrations/callback/google"
  },
  "Yelp": {
    "RedirectUri": "https://api.yourdomain.com/api/integrations/callback/yelp"
  },
  "Facebook": {
    "RedirectUri": "https://api.yourdomain.com/api/integrations/callback/facebook"
  },
  "App": {
    "FrontendUrl": "https://yourdomain.com"
  },
  "Cors": {
    "AllowedOrigins": ["https://yourdomain.com"]
  }
}
```

---

## 🔒 Security Best Practices

1. **Never commit secrets to Git**:
   - Use `appsettings.json` for local development
   - Use environment variables or Azure Key Vault in production
   - Keep `appsettings.example.json` as a template without secrets

2. **Use HTTPS in production**:
   - All OAuth redirect URIs must use HTTPS
   - SSL certificates required

3. **Validate state parameter**:
   - Prevents CSRF attacks
   - Already implemented in the OAuth callback handlers

4. **Rotate tokens regularly**:
   - Implement token refresh before expiration
   - Already implemented in all services

5. **Limit scope permissions**:
   - Only request the minimum scopes needed
   - Current scopes are appropriate for read-only review access

---

## 📊 Database Schema

OAuth tokens are stored in the `PlatformConnections` table:

```sql
CREATE TABLE PlatformConnections (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BusinessId INT NOT NULL,
    Platform INT NOT NULL, -- 0=Google, 1=Yelp, 2=Facebook
    AccessToken NVARCHAR(MAX),
    RefreshToken NVARCHAR(MAX),
    TokenExpiresAt DATETIME2,
    PlatformBusinessId NVARCHAR(255),
    PlatformBusinessName NVARCHAR(255),
    ConnectedAt DATETIME2 NOT NULL,
    LastSyncedAt DATETIME2,
    IsActive BIT NOT NULL DEFAULT 1,
    AutoSync BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (BusinessId) REFERENCES Businesses(Id)
);
```

---

## ❓ Troubleshooting

### "Invalid redirect URI" error

- Make sure the redirect URI in your platform settings exactly matches the one in your config
- Check for trailing slashes (should not have one)
- Ensure protocol matches (http vs https)

### "Token exchange failed" error

- Verify your Client ID and Client Secret are correct
- Check that your app has the required scopes enabled
- Review the API logs for detailed error messages

### "No reviews found" error

- For Google: Ensure you've claimed your business on Google My Business
- For Yelp: Make sure the business ID is correct (check Yelp URL)
- For Facebook: Verify you have admin access to the Facebook page

### Token refresh issues

- The refresh logic automatically runs before making API calls
- If refresh fails, user will need to reconnect the platform
- Check token expiration times in the database

---

## 📚 Additional Resources

- [Google My Business API Documentation](https://developers.google.com/my-business/content/overview)
- [Yelp Fusion API Documentation](https://www.yelp.com/developers/documentation/v3)
- [Facebook Graph API Documentation](https://developers.facebook.com/docs/graph-api)
- [OAuth 2.0 Specification](https://oauth.net/2/)

---

## 🎯 Next Steps

1. Set up OAuth credentials for all three platforms
2. Update your `appsettings.json` with the credentials
3. Test the OAuth flow locally
4. Implement additional platforms (TripAdvisor, Trustpilot, etc.)
5. Add automated review syncing with background jobs
6. Deploy to production and update redirect URIs

---

**Note**: Some platforms (especially Facebook) require app review before accessing certain features in production. Plan ahead for this approval process.
