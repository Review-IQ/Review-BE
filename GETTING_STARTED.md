# Getting Started with ReviewHub

Complete guide to set up ReviewHub from scratch in 15 minutes.

## 📋 Prerequisites

- ✅ .NET 9.0 SDK
- ✅ Node.js 18+ and npm
- ✅ SQL Server or LocalDB
- ✅ Git

## 🎯 Quick Start Options

### Option 1: Demo Mode (5 seconds) ⚡

**Perfect for:** Demos, UI testing, exploring features

```bash
cd client
copy .env.demo .env
npm install
npm run dev
```

Open http://localhost:5173 - Done! The entire app works with mock data.

### Option 2: Full Stack Development (15 minutes) 🚀

**Perfect for:** Development, testing with real database

Follow the sections below.

---

## Step 1: Clone & Setup (2 minutes)

```bash
# Navigate to project
cd C:\myStuff\ReviewHub

# Install frontend dependencies
cd client
npm install
cd ..
```

## Step 2: Database Setup (3 minutes)

### A. Update Connection String

Edit `src/ReviewHub.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ReviewHubDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### B. Run Migrations

```bash
cd src/ReviewHub.API
dotnet ef database update
```

This creates the database with all tables.

## Step 3: Auth0 Setup (5 minutes)

### A. Create Auth0 Application

1. Go to [Auth0 Dashboard](https://manage.auth0.com/)
2. Create new **Single Page Application**
3. Note your **Domain** and **Client ID**

### B. Configure Application

**Allowed Callback URLs:**
```
http://localhost:5173/callback
```

**Allowed Logout URLs:**
```
http://localhost:5173
```

**Allowed Web Origins:**
```
http://localhost:5173
```

### C. Create API

1. Go to Applications → APIs
2. Create new API
3. **Identifier:** `https://api.reviewhub.com`
4. Note the identifier

### D. Update Backend Config

Edit `src/ReviewHub.API/appsettings.json`:

```json
{
  "Auth0": {
    "Domain": "your-domain.auth0.com",
    "Audience": "https://api.reviewhub.com"
  }
}
```

### E. Update Frontend Config

Create `client/.env`:

```env
VITE_DEMO_MODE=false
VITE_API_URL=http://localhost:5000/api
VITE_AUTH0_DOMAIN=your-domain.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.reviewhub.com
```

## Step 4: Stripe Setup (Optional - 5 minutes)

### A. Get Stripe Keys

1. Go to [Stripe Dashboard](https://dashboard.stripe.com/)
2. Get **Secret Key** and **Publishable Key**
3. Go to **Products** and create 2 products:
   - **Pro Plan** - $49/month
   - **Enterprise Plan** - $149/month
4. Note the **Price IDs**

### B. Configure Stripe

Edit `src/ReviewHub.API/appsettings.json`:

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_...",
    "ProPriceId": "price_...",
    "EnterprisePriceId": "price_..."
  },
  "App": {
    "FrontendUrl": "http://localhost:5173"
  }
}
```

### C. Setup Webhook (After deployment)

1. Go to Stripe Dashboard → Developers → Webhooks
2. Add endpoint: `http://your-domain/api/webhook/stripe`
3. Select events:
   - `checkout.session.completed`
   - `customer.subscription.created`
   - `customer.subscription.updated`
   - `customer.subscription.deleted`
   - `invoice.payment_succeeded`
   - `invoice.payment_failed`

## Step 5: Twilio Setup (Optional - 3 minutes)

### A. Get Twilio Credentials

1. Go to [Twilio Console](https://console.twilio.com/)
2. Get **Account SID** and **Auth Token**
3. Get or purchase a **Phone Number**

### B. Configure Twilio

Edit `src/ReviewHub.API/appsettings.json`:

```json
{
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "...",
    "PhoneNumber": "+1..."
  }
}
```

## Step 6: Run the Application

### A. Start Backend

```bash
cd src/ReviewHub.API
dotnet run
```

Backend runs on: http://localhost:5000

### B. Start Frontend (New Terminal)

```bash
cd client
npm run dev
```

Frontend runs on: http://localhost:5173

### C. Open Browser

Navigate to http://localhost:5173

## 🎉 You're Done!

The application is now running with:
- ✅ Auth0 authentication
- ✅ Database with migrations
- ✅ API backend
- ✅ React frontend
- ✅ (Optional) Stripe subscriptions
- ✅ (Optional) Twilio SMS

---

## 🧪 Testing the Application

### 1. Register a New User

1. Click "Sign Up"
2. Complete Auth0 registration
3. Fill out profile completion form
4. You're redirected to dashboard

### 2. Add a Business

1. Go to Dashboard
2. Click "Add Business" (or create via Settings)
3. Fill in business details

### 3. Test Platform Integration (Mock)

1. Go to Integrations page
2. Click "Connect" on any platform
3. Mock OAuth flow completes

### 4. Test SMS (If Twilio configured)

1. Go to POS Automation
2. Add a customer with phone number
3. Send test SMS

### 5. Test Subscriptions (If Stripe configured)

1. Go to Settings → Billing
2. Click "Upgrade to Pro"
3. Complete Stripe checkout (use test card: 4242 4242 4242 4242)

---

## 🔧 Troubleshooting

### Database Connection Issues

**Error:** Cannot connect to database

**Solution:**
```bash
# Check SQL Server is running
# Update connection string in appsettings.json
# Run migrations again
dotnet ef database update
```

### Auth0 Login Redirect Loop

**Error:** Keeps redirecting to login

**Solution:**
- Check `VITE_AUTH0_DOMAIN` and `VITE_AUTH0_CLIENT_ID` are correct
- Verify callback URLs in Auth0 dashboard
- Clear browser cache and cookies

### CORS Errors

**Error:** CORS policy blocks request

**Solution:**
Edit `appsettings.json`:
```json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  }
}
```

### Port Already in Use

**Error:** Port 5000 or 5173 in use

**Solution:**
```bash
# Change backend port in launchSettings.json
# Change frontend port:
npm run dev -- --port 3000
```

### Migration Errors

**Error:** Migration fails

**Solution:**
```bash
# Remove migrations
dotnet ef migrations remove

# Re-create migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

---

## 📁 Project Structure

```
ReviewHub/
├── src/
│   ├── ReviewHub.API/          # Backend API
│   │   ├── Controllers/        # API endpoints
│   │   ├── Migrations/         # Database migrations
│   │   └── appsettings.json    # Configuration
│   ├── ReviewHub.Core/         # Domain entities
│   └── ReviewHub.Infrastructure/ # Services & DB
├── client/                     # React frontend
│   ├── src/
│   │   ├── components/         # React components
│   │   ├── pages/              # Page components
│   │   ├── services/           # API & mock services
│   │   └── auth/               # Auth0 integration
│   ├── .env.demo               # Demo mode config
│   └── .env                    # Your config
└── docs/                       # Documentation
```

---

## 🚀 Development Workflow

### Daily Development

```bash
# Terminal 1 - Backend
cd src/ReviewHub.API
dotnet watch run

# Terminal 2 - Frontend
cd client
npm run dev
```

Both will auto-reload on changes.

### Adding a New Migration

```bash
cd src/ReviewHub.API
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Building for Production

```bash
# Backend
cd src/ReviewHub.API
dotnet publish -c Release -o ./publish

# Frontend
cd client
npm run build
# Output in dist/
```

---

## 🎭 Switching Between Modes

### Enable Demo Mode

```env
VITE_DEMO_MODE=true
```

### Disable Demo Mode (Use Real Backend)

```env
VITE_DEMO_MODE=false
VITE_API_URL=http://localhost:5000/api
VITE_AUTH0_DOMAIN=your-domain.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.reviewhub.com
```

---

## 📚 Next Steps

### Immediate
- [ ] Set up Auth0
- [ ] Run database migrations
- [ ] Test authentication flow
- [ ] Add a test business

### Short-term
- [ ] Configure Stripe for subscriptions
- [ ] Set up Twilio for SMS
- [ ] Test all features end-to-end

### Long-term
- [ ] Implement Google OAuth for real reviews
- [ ] Add AI reply suggestions (OpenAI)
- [ ] Deploy to Azure
- [ ] Set up monitoring

---

## 🆘 Need Help?

- **Documentation:** Check `docs/` folder
- **API Reference:** `docs/API.md`
- **Auth Flow:** `docs/AUTH_FLOW.md`
- **Deployment:** `docs/DEPLOYMENT.md`
- **Demo Mode:** `docs/DEMO_MODE.md`

---

## ✨ Quick Tips

1. **Use Demo Mode First** - Get familiar with the UI before setting up Auth0
2. **Test with Stripe Test Mode** - Use test cards, don't use real payment info
3. **Check Browser Console** - Most errors will show here
4. **Use Swagger** - http://localhost:5000/swagger for API testing
5. **Check Logs** - Backend console shows detailed error messages

---

**Happy coding! 🎉**
