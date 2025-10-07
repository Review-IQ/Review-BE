# Authentication & User Registration Flow

## Overview

ReviewHub uses **Auth0** for authentication and maintains a local user database for business data and permissions. This dual-system approach ensures secure authentication while maintaining full control over user data and subscription management.

## Authentication Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         USER JOURNEY                             │
└─────────────────────────────────────────────────────────────────┘

1. User visits ReviewHub
   ↓
2. Clicks "Login" or "Sign Up"
   ↓
3. Redirected to Auth0 Login Page
   ↓
4. User authenticates (email/password, social login, etc.)
   ↓
5. Auth0 redirects back with JWT token
   ↓
6. Frontend calls GET /api/auth/me
   ↓
   ├─→ 200 OK: User exists and active
   │   └─→ Redirect to Dashboard
   │
   ├─→ 404 Not Found: User not in system
   │   └─→ Redirect to /register-complete
   │       └─→ User fills profile form
   │           └─→ POST /api/auth/register
   │               └─→ Redirect to Dashboard
   │
   └─→ 403 Forbidden: Account inactive
       └─→ Show "Account Inactive" error page
```

## Component Responsibilities

### Backend (AuthController.cs)

#### `POST /api/auth/register`
- **Purpose:** Create user record in our database after Auth0 signup
- **Auth:** Requires Auth0 JWT token
- **Logic:**
  1. Extract Auth0 ID from JWT claims (`ClaimTypes.NameIdentifier`)
  2. Check if user already exists with this Auth0 ID
  3. If exists → Return existing user
  4. If new → Create user record with `IsActive = true`, `SubscriptionPlan = "Free"`
  5. Save to database

#### `GET /api/auth/me`
- **Purpose:** Verify user exists and is active in our system
- **Auth:** Requires Auth0 JWT token
- **Logic:**
  1. Extract Auth0 ID from JWT claims
  2. Query database for user with this Auth0 ID
  3. If not found → Return 404 with `needsRegistration: true`
  4. If found but `IsActive = false` → Return 403 Forbidden
  5. If found and active → Return user profile

### Frontend (React)

#### `Login.tsx`
- Auth0 login page with "Log In" and "Sign Up" buttons
- Uses `loginWithRedirect()` from `@auth0/auth0-react`
- Handles Auth0 callback and token storage

#### `ProtectedRoute.tsx`
- Wrapper component for all authenticated routes
- **Flow:**
  1. Check if user is authenticated via Auth0
  2. If not → Redirect to `/login`
  3. If authenticated → Call `GET /api/auth/me`
  4. Based on response:
     - 200 OK → Allow access to route
     - 404 → Redirect to `/register-complete`
     - 403 → Show "Account Inactive" screen

#### `RegisterComplete.tsx`
- Registration completion form
- Pre-fills email from Auth0 user object
- Collects: Full Name (required), Company Name (optional), Phone Number (optional)
- Calls `POST /api/auth/register`
- Redirects to dashboard on success

#### `Auth0ProviderWithHistory.tsx`
- Wraps app with Auth0Provider
- Configures Auth0 domain, client ID, audience
- Sets up token retrieval for API calls
- Uses `Auth0TokenSetup` component to inject `getAccessTokenSilently` into API service

## User States

### 1. New User (First Time)
```
Auth0: ✅ Authenticated
Our DB: ❌ No record
State: Needs Registration
Action: Complete profile → Create record
```

### 2. Existing Active User
```
Auth0: ✅ Authenticated
Our DB: ✅ Exists, IsActive = true
State: Active
Action: Access dashboard
```

### 3. Inactive User
```
Auth0: ✅ Authenticated
Our DB: ✅ Exists, IsActive = false
State: Inactive
Action: Show error, contact support
```

### 4. Unauthenticated
```
Auth0: ❌ Not authenticated
State: Guest
Action: Redirect to login
```

## API Token Flow

```
Frontend Request
    ↓
Auth0TokenSetup (useEffect)
    ↓
Calls getAccessTokenSilently()
    ↓
Retrieves JWT from Auth0
    ↓
API Service Interceptor
    ↓
Adds header: Authorization: Bearer {token}
    ↓
Backend receives request
    ↓
JWT Middleware validates token
    ↓
Extracts Auth0 ID from claims
    ↓
Controller uses Auth0 ID to query user
```

## Security Features

1. **JWT Validation:** All API requests require valid Auth0 JWT token
2. **Active Check:** Every request verifies `IsActive = true`
3. **Auth0 ID Binding:** Users identified by Auth0 ID (unique, immutable)
4. **No Password Storage:** All password handling done by Auth0
5. **Token Refresh:** Uses refresh tokens for seamless re-authentication
6. **CORS Protection:** Only allowed origins can make API requests

## Configuration Required

### Auth0 Dashboard
1. Create Application (Single Page Application)
2. Configure:
   - Allowed Callback URLs
   - Allowed Logout URLs
   - Allowed Web Origins
   - JWT Expiration
3. Create API with identifier (audience)

### Environment Variables

**Backend (appsettings.json):**
```json
{
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "Audience": "https://api.reviewhub.com"
  }
}
```

**Frontend (.env):**
```env
VITE_AUTH0_DOMAIN=your-domain.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.reviewhub.com
```

## Testing the Flow

### Test New User Registration
1. Go to `/login`
2. Click "Sign Up"
3. Complete Auth0 registration
4. Should redirect to `/register-complete`
5. Fill profile form
6. Submit → Should redirect to `/dashboard`
7. Verify user exists in database with `IsActive = true`

### Test Existing User Login
1. Go to `/login`
2. Click "Log In"
3. Enter credentials
4. Should redirect directly to `/dashboard`

### Test Inactive Account
1. In database, set user's `IsActive = false`
2. Try to login
3. Should see "Account Inactive" error page

## Database Schema

```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Auth0Id NVARCHAR(255) NOT NULL UNIQUE,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    FullName NVARCHAR(255) NOT NULL,
    CompanyName NVARCHAR(255) NULL,
    PhoneNumber NVARCHAR(50) NULL,
    SubscriptionPlan NVARCHAR(50) NOT NULL DEFAULT 'Free',
    SubscriptionExpiresAt DATETIME2 NULL,
    StripeCustomerId NVARCHAR(255) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

CREATE INDEX IX_Users_Auth0Id ON Users(Auth0Id);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);
```

## Common Issues & Solutions

### Issue: User gets stuck in registration loop
**Cause:** Registration endpoint failing silently
**Solution:** Check backend logs, verify Auth0 token is valid, ensure database is accessible

### Issue: 401 Unauthorized on all API calls
**Cause:** JWT token not being sent or invalid
**Solution:** Verify `setTokenRetriever` is called, check Auth0 configuration matches backend

### Issue: User shows as needing registration even after completing form
**Cause:** Registration call failed or user record not created
**Solution:** Check database for user record, verify Auth0 ID matches token

### Issue: CORS errors on login
**Cause:** Auth0 allowed origins not configured
**Solution:** Add frontend URL to Auth0 allowed origins in dashboard

## Next Steps

After basic auth is working:
1. Add email verification requirement
2. Implement subscription plan enforcement
3. Add password reset flow
4. Implement MFA (Multi-Factor Authentication)
5. Add user roles and permissions
6. Implement account deletion flow
