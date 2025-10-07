# Demo Mode Guide

## Overview

ReviewHub includes a **Demo Mode** that allows you to run the entire application with mock data, **without requiring Auth0, a backend API, or a database**. This is perfect for:

- 🎨 **Product demos** - Show off the full app to potential clients
- 🧪 **Testing UI/UX** - Try out features without setting up infrastructure
- 📸 **Screenshots** - Capture marketing materials
- 🎓 **Training** - Let team members explore the interface
- 🚀 **Quick evaluation** - See all features working instantly

## Quick Start (5 seconds!)

### Method 1: Copy the demo config

```bash
cd client
copy .env.demo .env
npm run dev
```

### Method 2: Set environment variable

```bash
cd client
echo VITE_DEMO_MODE=true > .env
npm run dev
```

**That's it!** Open http://localhost:5173 and the app will work with full mock data.

## What Works in Demo Mode

✅ **Authentication** - Auto-logged in as "Demo User"
✅ **All Pages** - Dashboard, Reviews, Integrations, Analytics, POS Automation, Competitors, Settings
✅ **Mock Data** - 2 businesses, 5 reviews, 4 platforms, 3 customers, 2 campaigns, 2 competitors
✅ **Interactive Features** - Click buttons, submit forms, see loading states
✅ **Realistic Delays** - API calls simulate 300ms network latency
✅ **Console Logging** - All "API calls" logged to browser console

## Demo Mode Features

### Mock User Profile
- **Name:** Demo User
- **Email:** demo@reviewhub.com
- **Company:** Demo Restaurant Group
- **Subscription:** Pro Plan

### Mock Businesses
1. **Main Street Cafe** - 247 reviews, 4.5 avg rating
2. **Downtown Pizzeria** - 189 reviews, 4.7 avg rating

### Mock Reviews
- 5 sample reviews across different platforms
- Mix of positive, mixed, and negative sentiments
- Some flagged, some unread
- AI-suggested responses included

### Mock Platforms
- Google Business Profile (connected)
- Yelp (connected)
- TripAdvisor (connected)
- Facebook (not connected)

## Switching Between Modes

### Enable Demo Mode

**Option 1: Environment Variable**
```env
VITE_DEMO_MODE=true
```

**Option 2: Copy Demo Config**
```bash
copy .env.demo .env
```

### Disable Demo Mode (Use Real Auth0/Backend)

```env
VITE_DEMO_MODE=false
VITE_AUTH0_DOMAIN=your-domain.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.reviewhub.com
```

## Visual Indicators

When demo mode is enabled, you'll see:

1. **Console Message** - Colorful banner in browser console:
   ```
   🎭 DEMO MODE ENABLED
   All API calls will use mock data. No backend or Auth0 required!
   ```

2. **API Call Logging** - Every "API call" is logged:
   ```
   [MOCK API] Get businesses
   [MOCK API] Get reviews: {...}
   [MOCK API] Reply to review: 1 Thank you!
   ```

## Demo Mode Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    React Frontend                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  ┌────────────────┐      ┌────────────────────────┐   │
│  │   Components   │─────▶│    Auth Provider       │   │
│  └────────────────┘      │                        │   │
│           │              │  Demo: MockAuth0       │   │
│           │              │  Prod: Real Auth0      │   │
│           │              └────────────────────────┘   │
│           │                                            │
│           ▼                                            │
│  ┌────────────────┐      ┌────────────────────────┐   │
│  │   API Service  │─────▶│    Data Source         │   │
│  └────────────────┘      │                        │   │
│                          │  Demo: mockData.ts     │   │
│                          │  Prod: Axios + Backend │   │
│                          └────────────────────────┘   │
└─────────────────────────────────────────────────────────┘

NO BACKEND NEEDED IN DEMO MODE ✅
NO AUTH0 NEEDED IN DEMO MODE ✅
NO DATABASE NEEDED IN DEMO MODE ✅
```

## Customizing Mock Data

Want to customize the demo data? Edit these files:

### `client/src/services/mockData.ts`

```typescript
export const mockBusinesses = [
  {
    id: 1,
    name: 'YOUR BUSINESS NAME',
    industry: 'YOUR INDUSTRY',
    avgRating: 4.8,
    reviewsCount: 500,
    // ... add more fields
  }
];

export const mockReviews = [
  {
    id: 1,
    reviewerName: 'YOUR REVIEWER',
    rating: 5,
    reviewText: 'YOUR REVIEW TEXT',
    platform: 'Google',
    // ... add more fields
  }
];
```

### Add More Data

You can add as much mock data as you want:

```typescript
// Add 100 businesses
export const mockBusinesses = Array.from({ length: 100 }, (_, i) => ({
  id: i + 1,
  name: `Business ${i + 1}`,
  avgRating: 4.0 + Math.random(),
  reviewsCount: Math.floor(Math.random() * 500),
  // ...
}));

// Add 1000 reviews
export const mockReviews = Array.from({ length: 1000 }, (_, i) => ({
  id: i + 1,
  reviewerName: `Reviewer ${i + 1}`,
  rating: Math.floor(Math.random() * 5) + 1,
  reviewText: `This is review number ${i + 1}`,
  // ...
}));
```

## Demo Mode API Delays

All mock API calls include realistic delays:

```typescript
// Default delay: 300ms
await delay();

// Longer operations: 500ms
await delay(500);

// Customize in mockApi.ts
const delay = (ms: number = 300) => new Promise(resolve => setTimeout(resolve, ms));
```

## Limitations

Demo mode has a few limitations:

❌ **No Real OAuth** - Can't actually connect to Google, Yelp, etc.
❌ **No Data Persistence** - Refreshing the page resets all changes
❌ **No Real SMS** - Can't send actual text messages via Twilio
❌ **No Real Payments** - Can't process Stripe subscriptions
❌ **No File Uploads** - Can't upload logos or documents

For these features, you need the full backend setup.

## Production Checklist

Before deploying to production, make sure:

- [ ] `VITE_DEMO_MODE=false` in production .env
- [ ] Real Auth0 credentials configured
- [ ] Backend API URL set correctly
- [ ] Database migrations run
- [ ] Stripe webhooks configured
- [ ] Twilio credentials set

## Use Cases

### 1. Sales Demo
```bash
# Set up demo mode
copy .env.demo .env
npm run dev

# Show features:
✅ Dashboard with charts and stats
✅ Review management with AI replies
✅ Platform integrations
✅ SMS campaigns
✅ Competitor tracking
```

### 2. UI/UX Testing
```bash
# Enable demo mode
VITE_DEMO_MODE=true

# Test:
- Responsive design
- Navigation flows
- Form validation
- Loading states
- Error handling
```

### 3. Development
```bash
# Work on frontend without backend
VITE_DEMO_MODE=true
npm run dev

# Make UI changes, see them instantly
# No need to run .NET backend or database
```

## Troubleshooting

### Issue: App still tries to connect to Auth0
**Solution:** Make sure `VITE_DEMO_MODE=true` is in your `.env` file (not `.env.example`)

### Issue: API calls are failing
**Solution:** Check browser console for errors. Make sure mock data imports are correct.

### Issue: Navigation is broken
**Solution:** Verify all components use the conditional auth hook:
```typescript
const auth0 = IS_DEMO_MODE ? useMockAuth0() : useAuth0();
```

### Issue: Environment variable not working
**Solution:** Restart the dev server after changing `.env` file:
```bash
# Stop the server (Ctrl+C)
npm run dev
```

## FAQ

**Q: Can I use demo mode in production?**
A: No, demo mode is for testing/demos only. Use real Auth0 and backend in production.

**Q: Does demo mode work with the built version?**
A: Yes! Build with `VITE_DEMO_MODE=true` and the built app will use mock data.

**Q: Can I customize the demo user?**
A: Yes, edit `MockAuth0Provider.tsx` and change the `mockUser` object.

**Q: How do I add more mock reviews?**
A: Edit `mockData.ts` and add more objects to the `mockReviews` array.

**Q: Can I deploy a demo version?**
A: Yes! Build with demo mode enabled and deploy to any static hosting (Netlify, Vercel, Azure Static Web Apps).

## Demo Mode Deployment

Deploy a public demo:

```bash
# Build with demo mode
cd client
echo VITE_DEMO_MODE=true > .env
npm run build

# Deploy to Netlify
netlify deploy --prod --dir=dist

# Or deploy to Azure Static Web Apps
swa deploy ./dist --env production
```

Now anyone can try your app without Auth0 or a backend!

---

**Demo mode makes ReviewHub instantly accessible for demos, testing, and exploration. Enjoy! 🎉**
