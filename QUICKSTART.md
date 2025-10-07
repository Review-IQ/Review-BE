# ReviewHub - Quick Start Guide

Get your ReviewHub development environment up and running in 10 minutes!

## Prerequisites

Before you begin, make sure you have these installed:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/) (includes npm)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) (comes with Visual Studio)

## Quick Setup (5 Steps)

### 1. Clone or Navigate to Project

```bash
cd C:\myStuff\ReviewHub
```

### 2. Setup Backend Database

```bash
cd src/ReviewHub.API
dotnet ef database update
```

Expected output:
```
Build started...
Build succeeded.
Applying migration '20240120_InitialCreate'.
Done.
```

### 3. Configure Backend (Optional)

The backend works with default settings, but you can customize:

```bash
# Copy example config
cp appsettings.example.json appsettings.json

# Edit appsettings.json with your credentials (optional)
# - Auth0, Stripe, Twilio, Mailgun credentials
```

### 4. Setup Frontend

```bash
cd ../../client
npm install
```

Expected output:
```
added 500+ packages in 30s
```

### 5. Start the Application

**Option A - Using PowerShell Script (Recommended):**

```powershell
cd ..
.\start-dev.ps1
```

This will open two windows:
- Backend API on http://localhost:5000
- Frontend on http://localhost:5173

**Option B - Manual Start (Two Terminals):**

Terminal 1 - Backend:
```bash
cd src/ReviewHub.API
dotnet run
```

Terminal 2 - Frontend:
```bash
cd client
npm run dev
```

## Access the Application

Once started, open your browser to:

- **Frontend App:** http://localhost:5173
- **Swagger API Docs:** http://localhost:5000/swagger
- **API Base URL:** http://localhost:5000/api

## Test the Application

### Using Mock Data

The application comes with mock data for all features:

1. **Dashboard:** View stats, charts, and recent activity
2. **Reviews:** See sample reviews from Google, Yelp, Facebook
3. **Integrations:** Explore platform connection UI
4. **Analytics:** Interactive charts with sample data
5. **POS Automation:** Customer management interface
6. **Competitors:** Competitor tracking features
7. **Settings:** User settings and preferences

### Test Mobile Responsiveness

1. Open Chrome DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M)
3. Select a mobile device (iPhone, Pixel, etc.)

## Default Features Working

✅ **Working with Mock Data:**
- Dashboard with statistics
- Review listing and filtering
- Platform integration UI
- Analytics charts
- Competitor tracking
- Settings pages
- Mobile responsive design

⏳ **Requires Configuration:**
- Auth0 authentication
- Real OAuth for platforms
- Stripe payments
- Twilio SMS
- Mailgun emails

## Next Steps

### 1. Connect Auth0

```bash
cd client
```

Create `.env` file:
```
VITE_AUTH0_DOMAIN=your-domain.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.reviewhub.com
VITE_API_URL=http://localhost:5000/api
```

Install Auth0:
```bash
npm install @auth0/auth0-react
```

### 2. Configure External Services

Update `src/ReviewHub.API/appsettings.json`:

```json
{
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "Audience": "https://api.reviewhub.com"
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_..."
  },
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "...",
    "PhoneNumber": "+1234567890"
  }
}
```

### 3. Implement Real OAuth

Replace mock OAuth in `IntegrationsController.cs`:
- Google Business Profile API
- Yelp Fusion API
- Facebook Graph API

## Common Issues & Solutions

### Issue: Database Error

**Error:** `Cannot open database "ReviewHubDb"`

**Solution:**
```bash
cd src/ReviewHub.API
dotnet ef database update --force
```

### Issue: Port Already in Use

**Error:** `Address already in use localhost:5000`

**Solution:**
```bash
# Find and kill process using port 5000
netstat -ano | findstr :5000
taskkill /PID <process-id> /F
```

### Issue: Node Modules Error

**Error:** `Module not found` or `Cannot find module`

**Solution:**
```bash
cd client
rm -rf node_modules package-lock.json
npm install
```

### Issue: Build Errors

**Error:** TypeScript or Vite build errors

**Solution:**
```bash
cd client
npm run build
```

## Project Structure

```
ReviewHub/
├── src/
│   ├── ReviewHub.API/          # Backend API
│   │   ├── Controllers/        # API endpoints
│   │   ├── Migrations/         # Database migrations
│   │   └── appsettings.json    # Configuration (gitignored)
│   ├── ReviewHub.Core/         # Domain entities
│   └── ReviewHub.Infrastructure/# Data access
├── client/                     # React frontend
│   ├── src/
│   │   ├── components/         # Reusable components
│   │   ├── pages/              # Page components
│   │   ├── services/           # API client
│   │   └── App.tsx             # Main app
│   ├── .env                    # Environment vars (gitignored)
│   └── package.json
├── docs/                       # Documentation
├── start-dev.ps1               # Development startup script
└── README.md
```

## Development Workflow

1. **Make changes** to code
2. **Hot reload** works automatically:
   - Frontend: Vite hot module replacement
   - Backend: File watcher (dotnet watch)
3. **Test** in browser at localhost:5173
4. **Check API** at localhost:5000/swagger

## Build for Production

### Backend

```bash
cd src/ReviewHub.API
dotnet publish -c Release -o ./publish
```

### Frontend

```bash
cd client
npm run build
```

Output in `client/dist/` directory.

## Get Help

- **Documentation:** See `README.md` and `docs/API.md`
- **Mobile Guide:** See `MOBILE_IMPROVEMENTS.md`
- **Issues:** Check common issues above
- **API Reference:** http://localhost:5000/swagger

## What's Included

### 7 Complete Pages
1. Dashboard - Stats and overview
2. Reviews - Unified review management
3. Integrations - Platform connections
4. Analytics - Charts and insights
5. POS Automation - SMS campaigns
6. Competitors - Tracking and comparison
7. Settings - User preferences

### 4 API Controllers
1. AuthController - User management
2. BusinessesController - Business CRUD
3. ReviewsController - Review operations
4. IntegrationsController - OAuth flows

### Database
- 10 entity types
- EF Core migrations
- SQL Server LocalDB

### Features
- Clean Architecture
- JWT Authentication ready
- Mobile responsive
- TypeScript frontend
- Tailwind CSS styling

---

**Ready to build?** Start developing with `.\start-dev.ps1` and explore the app! 🚀
