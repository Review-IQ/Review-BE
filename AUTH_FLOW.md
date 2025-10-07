# Backend-Based Auth0 Authentication Flow

## Overview

Auth0 authentication is now handled entirely on the backend for enhanced security. The client ID and client secret are stored securely on the backend, not exposed in the frontend.

## Architecture

### Backend (C# .NET)
- **Auth0 credentials**: Stored in `appsettings.json` (secure server-side configuration)
- **Authentication Controller**: `ReviewHub.API/Controllers/AuthController.cs`
- **JWT Validation**: Configured in `Program.cs` using `JwtBearer` middleware

### Frontend (React)
- **No Auth0 SDK**: Removed `@auth0/auth0-react` dependency
- **Simple Auth Context**: `client/src/auth/AuthContext.tsx`
- **Token Storage**: localStorage (with expiry tracking)
- **No secrets**: Only API URL is in frontend config

## Authentication Flow

### 1. User Clicks "Sign In"

```
Frontend (Login.tsx)
└─> Calls: auth.login()
    └─> Redirects to: GET /api/auth/login
```

### 2. Backend Initiates Auth0 Flow

```
Backend (AuthController.Login)
└─> Builds Auth0 authorization URL with:
    - domain (from appsettings.json)
    - clientId (from appsettings.json)
    - audience (from appsettings.json)
    - redirectUri: {backend}/api/auth/callback
└─> Redirects user to Auth0 login page
```

### 3. User Authenticates with Auth0

```
User enters credentials on Auth0 hosted login page
└─> Auth0 validates credentials
    └─> Redirects back to: {backend}/api/auth/callback?code=XXX
```

### 4. Backend Exchanges Code for Tokens

```
Backend (AuthController.Callback)
└─> Receives authorization code from Auth0
└─> Exchanges code for tokens using:
    - clientId (server-side)
    - clientSecret (server-side, never exposed)
    - code (from Auth0 redirect)
└─> Receives:
    - access_token (JWT)
    - id_token
    - expires_in
└─> Redirects to frontend: {frontend}/auth/callback?access_token=XXX&...
```

### 5. Frontend Stores Tokens

```
Frontend (AuthCallback component)
└─> Extracts tokens from URL parameters
└─> Stores in localStorage:
    - access_token
    - id_token (optional)
    - token_expiry (calculated from expires_in)
└─> Redirects user to: /dashboard (or original requested page)
```

### 6. API Requests Include Token

```
Frontend (api.ts axios interceptor)
└─> For each API request:
    └─> Gets access_token from localStorage
    └─> Adds header: Authorization: Bearer {access_token}

Backend (JWT Middleware)
└─> Validates JWT token against Auth0
└─> Extracts user claims (auth0Id, email, etc.)
└─> Allows/denies request
```

### 7. Logout Flow

```
Frontend (logout button)
└─> Clears localStorage
└─> Redirects to: GET /api/auth/logout

Backend (AuthController.Logout)
└─> Redirects to Auth0 logout URL
    └─> Clears Auth0 session
    └─> Redirects back to: {frontend}/login
```

## Security Benefits

### ✅ What's Improved

1. **Client Secret Protection**: Never exposed to frontend/browser
2. **Token Exchange Security**: Happens server-to-server (Auth0 ↔ Backend)
3. **Reduced Attack Surface**: Frontend has no Auth0 configuration
4. **Centralized Auth Logic**: All auth flows controlled by backend
5. **Audit Trail**: Backend can log all authentication events

### 🔒 Backend Configuration (appsettings.json)

```json
{
  "Auth0": {
    "Domain": "YOUR_DOMAIN.auth0.com",
    "Audience": "https://api.reviewhub.com",
    "ClientId": "YOUR_CLIENT_ID",
    "ClientSecret": "YOUR_CLIENT_SECRET"  // ← Never exposed to frontend
  }
}
```

### 🌐 Frontend Configuration (.env)

```bash
# Only public configuration - no secrets
VITE_API_URL=http://localhost:5000/api
VITE_APP_NAME=ReviewHub
VITE_APP_URL=http://localhost:5173
```

## File Structure

### Backend Files
```
src/ReviewHub.API/
├── Controllers/AuthController.cs      # Login, callback, logout endpoints
├── Program.cs                          # JWT authentication setup
└── appsettings.json                    # Auth0 credentials (DO NOT COMMIT)
```

### Frontend Files
```
client/src/
├── auth/
│   ├── AuthContext.tsx                 # New simple auth context
│   └── MockAuth0Provider.tsx           # Demo mode provider
├── pages/Login.tsx                     # Updated to use new auth
├── components/ProtectedRoute.tsx       # Updated to use new auth
└── services/api.ts                     # Token injection from localStorage
```

## Auth0 Configuration Required

### Allowed Callback URLs
Add to your Auth0 Application settings:
```
http://localhost:5000/api/auth/callback
https://your-backend-domain.com/api/auth/callback
```

### Allowed Logout URLs
```
http://localhost:5173/login
https://your-frontend-domain.com/login
```

### Application Type
- **Regular Web Application** (not SPA)
- Enables use of client secret for server-side token exchange

## Demo Mode

Demo mode still works exactly as before:
```bash
# .env
VITE_DEMO_MODE=true
```

When enabled:
- Uses `MockAuth0Provider` instead of `AuthContext`
- No backend or Auth0 required
- All data from `mockData.ts`

## Migration from Old Flow

### Removed Dependencies
```bash
# package.json - can remove:
"@auth0/auth0-react": "^2.5.0"
```

### Removed Files
- `src/auth/Auth0ProviderWithHistory.tsx` (no longer needed)

### Updated Files
- `App.tsx`: Uses `AuthProvider` instead of `Auth0ProviderWithHistory`
- `Login.tsx`: Uses `useAuth()` instead of `useAuth0()`
- `ProtectedRoute.tsx`: Uses `useAuth()` instead of `useAuth0()`
- `api.ts`: Gets token from localStorage instead of Auth0 hook

## Testing

### 1. Start Backend
```bash
cd src/ReviewHub.API
dotnet run
```

### 2. Configure Auth0 in Backend
Edit `appsettings.json` with real Auth0 credentials

### 3. Start Frontend (Production Mode)
```bash
cd client
# Set VITE_DEMO_MODE=false in .env
npm run dev
```

### 4. Test Flow
1. Navigate to http://localhost:5173
2. Click "Sign In Securely"
3. Should redirect to: `http://localhost:5000/api/auth/login`
4. Backend redirects to Auth0 login
5. After auth, redirects back through backend
6. Backend exchanges code for token (using secret)
7. Redirects to frontend with token
8. Frontend stores token and redirects to dashboard

## Troubleshooting

### Issue: "Unauthorized" errors
- Check that `appsettings.json` has correct Auth0 Domain and Audience
- Verify token is being sent in Authorization header

### Issue: Login redirects to error page
- Check Auth0 Allowed Callback URLs include your backend callback
- Verify Auth0 credentials (ClientId, ClientSecret) are correct

### Issue: Token exchange fails
- Check that Application Type is "Regular Web Application" in Auth0
- Verify Client Secret is correct in `appsettings.json`

## Production Deployment

### Backend
1. Store Auth0 credentials in environment variables (not appsettings.json)
2. Use Azure Key Vault, AWS Secrets Manager, or similar
3. Never commit `appsettings.Production.json` with real credentials

### Frontend
1. Update `VITE_API_URL` to production backend URL
2. Ensure `VITE_DEMO_MODE=false`
3. No other secrets needed!

### Auth0 Configuration
1. Add production callback URLs to Auth0
2. Add production logout URLs to Auth0
3. Enable MFA for production users (recommended)
