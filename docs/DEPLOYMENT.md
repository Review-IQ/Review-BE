# ReviewHub Deployment Guide

Complete guide for deploying ReviewHub to production on Azure.

## Table of Contents

1. [Azure Resources Setup](#azure-resources-setup)
2. [Database Deployment](#database-deployment)
3. [Backend API Deployment](#backend-api-deployment)
4. [Frontend Deployment](#frontend-deployment)
5. [Environment Configuration](#environment-configuration)
6. [Post-Deployment Setup](#post-deployment-setup)
7. [CI/CD Pipeline](#cicd-pipeline)
8. [Monitoring & Logging](#monitoring--logging)

---

## Azure Resources Setup

### Required Azure Services

1. **Azure SQL Database** - Production database
2. **Azure App Service (Windows)** - .NET API hosting
3. **Azure Static Web Apps** - React frontend hosting
4. **Azure Key Vault** - Secrets management
5. **Azure Application Insights** - Monitoring and logging
6. **Azure Storage Account** - File uploads (optional)

### 1. Create Resource Group

```bash
az group create \
  --name rg-reviewhub-prod \
  --location eastus
```

### 2. Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name reviewhub-sql-server \
  --resource-group rg-reviewhub-prod \
  --location eastus \
  --admin-user sqladmin \
  --admin-password 'YourStrongPassword123!'

# Create Database
az sql db create \
  --resource-group rg-reviewhub-prod \
  --server reviewhub-sql-server \
  --name ReviewHubDb \
  --service-objective S0 \
  --backup-storage-redundancy Local

# Configure Firewall (Allow Azure Services)
az sql server firewall-rule create \
  --resource-group rg-reviewhub-prod \
  --server reviewhub-sql-server \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

**Connection String:**
```
Server=tcp:reviewhub-sql-server.database.windows.net,1433;Initial Catalog=ReviewHubDb;Persist Security Info=False;User ID=sqladmin;Password=YourStrongPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 3. Create App Service for API

```bash
# Create App Service Plan
az appservice plan create \
  --name reviewhub-api-plan \
  --resource-group rg-reviewhub-prod \
  --sku B1 \
  --is-linux false

# Create Web App
az webapp create \
  --resource-group rg-reviewhub-prod \
  --plan reviewhub-api-plan \
  --name reviewhub-api \
  --runtime "DOTNET:9.0"

# Configure Always On
az webapp config set \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api \
  --always-on true
```

### 4. Create Key Vault

```bash
az keyvault create \
  --name reviewhub-keyvault \
  --resource-group rg-reviewhub-prod \
  --location eastus

# Grant App Service access to Key Vault
az webapp identity assign \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api

# Get the principal ID from the output and use it here
az keyvault set-policy \
  --name reviewhub-keyvault \
  --object-id <principal-id> \
  --secret-permissions get list
```

### 5. Create Application Insights

```bash
az monitor app-insights component create \
  --app reviewhub-insights \
  --location eastus \
  --resource-group rg-reviewhub-prod \
  --application-type web
```

---

## Database Deployment

### 1. Run Migrations

From your local machine with VPN/firewall access:

```bash
cd src/ReviewHub.API

# Update connection string in appsettings.Production.json
dotnet ef database update --environment Production
```

Or use Azure SQL Migration tool or SQL Server Management Studio.

### 2. Verify Database

```bash
# Connect to Azure SQL and verify tables
sqlcmd -S reviewhub-sql-server.database.windows.net -U sqladmin -P YourStrongPassword123! -d ReviewHubDb -Q "SELECT name FROM sys.tables"
```

Expected tables:
- Users
- Businesses
- PlatformConnections
- Reviews
- SmsMessages
- Competitors
- __EFMigrationsHistory

---

## Backend API Deployment

### 1. Prepare for Deployment

Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://reviewhub-keyvault.vault.azure.net/secrets/SqlConnectionString/)"
  },
  "Auth0": {
    "Domain": "@Microsoft.KeyVault(SecretUri=https://reviewhub-keyvault.vault.azure.net/secrets/Auth0Domain/)",
    "Audience": "@Microsoft.KeyVault(SecretUri=https://reviewhub-keyvault.vault.azure.net/secrets/Auth0Audience/)"
  },
  "Stripe": {
    "SecretKey": "@Microsoft.KeyVault(SecretUri=https://reviewhub-keyvault.vault.azure.net/secrets/StripeSecretKey/)"
  },
  "Cors": {
    "AllowedOrigins": [
      "https://reviewhub.azurestaticapps.net",
      "https://www.reviewhub.com"
    ]
  }
}
```

### 2. Store Secrets in Key Vault

```bash
# SQL Connection String
az keyvault secret set \
  --vault-name reviewhub-keyvault \
  --name SqlConnectionString \
  --value "Server=tcp:reviewhub-sql-server.database.windows.net,1433;..."

# Auth0 Credentials
az keyvault secret set \
  --vault-name reviewhub-keyvault \
  --name Auth0Domain \
  --value "your-domain.auth0.com"

az keyvault secret set \
  --vault-name reviewhub-keyvault \
  --name Auth0Audience \
  --value "https://api.reviewhub.com"

# Stripe
az keyvault secret set \
  --vault-name reviewhub-keyvault \
  --name StripeSecretKey \
  --value "sk_live_..."

# Twilio
az keyvault secret set \
  --vault-name reviewhub-keyvault \
  --name TwilioAccountSid \
  --value "AC..."

# Continue for all secrets...
```

### 3. Build and Publish

```bash
cd src/ReviewHub.API

# Build in Release mode
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
Compress-Archive -Path * -DestinationPath ../reviewhub-api.zip
```

### 4. Deploy to App Service

**Option A - Using Azure CLI:**

```bash
az webapp deployment source config-zip \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api \
  --src reviewhub-api.zip
```

**Option B - Using Visual Studio:**

1. Right-click on `ReviewHub.API` project
2. Select "Publish"
3. Choose "Azure"
4. Select your App Service
5. Click "Publish"

**Option C - Using GitHub Actions (see CI/CD section)**

### 5. Configure App Service Settings

```bash
# Set environment
az webapp config appsettings set \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api \
  --settings ASPNETCORE_ENVIRONMENT=Production

# Set Application Insights
az webapp config appsettings set \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=..."
```

### 6. Test API

```bash
# Test health endpoint
curl https://reviewhub-api.azurewebsites.net/health

# Test Swagger
open https://reviewhub-api.azurewebsites.net/swagger
```

---

## Frontend Deployment

### 1. Create Static Web App

```bash
az staticwebapp create \
  --name reviewhub-frontend \
  --resource-group rg-reviewhub-prod \
  --location eastus2 \
  --sku Free
```

### 2. Configure Environment Variables

Create `client/.env.production`:

```env
VITE_API_URL=https://reviewhub-api.azurewebsites.net/api
VITE_AUTH0_DOMAIN=your-domain.auth0.com
VITE_AUTH0_CLIENT_ID=your-client-id
VITE_AUTH0_AUDIENCE=https://api.reviewhub.com
VITE_STRIPE_PUBLISHABLE_KEY=pk_live_...
```

### 3. Build Frontend

```bash
cd client

# Install dependencies
npm ci

# Build for production
npm run build
```

Output will be in `client/dist/`

### 4. Deploy to Static Web App

**Option A - Using Azure CLI:**

```bash
# Install SWA CLI
npm install -g @azure/static-web-apps-cli

# Deploy
swa deploy ./dist \
  --deployment-token <deployment-token> \
  --env production
```

**Option B - Using GitHub Actions (see CI/CD section)**

**Option C - Manual Upload:**

```bash
az staticwebapp upload \
  --name reviewhub-frontend \
  --resource-group rg-reviewhub-prod \
  --source ./dist
```

### 5. Configure Custom Domain

```bash
# Add custom domain
az staticwebapp hostname set \
  --name reviewhub-frontend \
  --resource-group rg-reviewhub-prod \
  --hostname www.reviewhub.com

# SSL is automatically configured
```

---

## Environment Configuration

### Auth0 Configuration

1. Go to Auth0 Dashboard
2. Create Production Application
3. Configure:
   - **Allowed Callback URLs:** `https://www.reviewhub.com/callback`
   - **Allowed Logout URLs:** `https://www.reviewhub.com`
   - **Allowed Web Origins:** `https://www.reviewhub.com`
   - **Allowed Origins (CORS):** `https://www.reviewhub.com`
4. Create API in Auth0:
   - **Identifier:** `https://api.reviewhub.com`
   - **Token Expiration:** 86400

### Stripe Configuration

1. Switch to Live mode in Stripe Dashboard
2. Get Live API keys
3. Configure webhooks:
   - **Endpoint URL:** `https://reviewhub-api.azurewebsites.net/api/webhooks/stripe`
   - **Events:**
     - `customer.subscription.created`
     - `customer.subscription.updated`
     - `customer.subscription.deleted`
     - `invoice.payment_succeeded`
     - `invoice.payment_failed`

### Twilio Configuration

1. Upgrade to paid Twilio account
2. Purchase phone number
3. Configure messaging webhook:
   - **URL:** `https://reviewhub-api.azurewebsites.net/api/webhooks/twilio`

---

## Post-Deployment Setup

### 1. Database Indexes

```sql
-- Add performance indexes
CREATE INDEX IX_Reviews_BusinessId_ReviewDate
ON Reviews(BusinessId, ReviewDate DESC);

CREATE INDEX IX_Reviews_Platform_IsRead
ON Reviews(Platform, IsRead);

CREATE INDEX IX_PlatformConnections_BusinessId_IsActive
ON PlatformConnections(BusinessId, IsActive);
```

### 2. Test End-to-End

1. **Registration:** Create a test user
2. **Authentication:** Login with Auth0
3. **Business Creation:** Add a test business
4. **Platform Connection:** Test OAuth flow
5. **Review Management:** Test CRUD operations
6. **Payments:** Test Stripe subscription
7. **SMS:** Send test message via Twilio

### 3. Monitor Deployment

```bash
# View logs
az webapp log tail \
  --name reviewhub-api \
  --resource-group rg-reviewhub-prod

# Check health
curl https://reviewhub-api.azurewebsites.net/health
```

---

## CI/CD Pipeline

### GitHub Actions Workflow

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]
  workflow_dispatch:

jobs:
  build-and-deploy-api:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Build API
      run: dotnet publish src/ReviewHub.API -c Release -o ./api-publish

    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'reviewhub-api'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./api-publish

  build-and-deploy-frontend:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v3

    - name: Setup Node
      uses: actions/setup-node@v3
      with:
        node-version: '18'

    - name: Build Frontend
      run: |
        cd client
        npm ci
        npm run build

    - name: Deploy to Static Web App
      uses: Azure/static-web-apps-deploy@v1
      with:
        azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_TOKEN }}
        repo_token: ${{ secrets.GITHUB_TOKEN }}
        action: 'upload'
        app_location: 'client'
        output_location: 'dist'
```

---

## Monitoring & Logging

### Application Insights Queries

```kusto
// API Errors
exceptions
| where timestamp > ago(1h)
| summarize count() by type, outerMessage

// Slow Requests
requests
| where duration > 1000
| project timestamp, name, duration, resultCode

// Platform Integrations
customEvents
| where name == "PlatformSync"
| summarize count() by tostring(customDimensions.Platform)
```

### Set Up Alerts

```bash
# High error rate alert
az monitor metrics alert create \
  --name api-high-error-rate \
  --resource-group rg-reviewhub-prod \
  --scopes /subscriptions/{subscription-id}/resourceGroups/rg-reviewhub-prod/providers/Microsoft.Web/sites/reviewhub-api \
  --condition "count requests/failed > 10" \
  --window-size 5m \
  --evaluation-frequency 1m
```

---

## Cost Estimation

**Monthly Costs (Production):**

- Azure SQL Database (S0): ~$15
- App Service (B1): ~$13
- Static Web Apps: Free tier
- Application Insights: ~$5
- Key Vault: ~$1
- **Total: ~$34/month** (excluding bandwidth and storage)

---

## Rollback Procedure

If deployment fails:

```bash
# 1. Swap deployment slots (if configured)
az webapp deployment slot swap \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api \
  --slot staging \
  --target-slot production

# 2. Or deploy previous version
az webapp deployment source config-zip \
  --resource-group rg-reviewhub-prod \
  --name reviewhub-api \
  --src reviewhub-api-v1.0.0.zip
```

---

## Security Checklist

- [ ] All secrets in Key Vault
- [ ] HTTPS enforced on all endpoints
- [ ] CORS configured correctly
- [ ] SQL firewall rules restrictive
- [ ] Auth0 production credentials
- [ ] Stripe webhook signature verification
- [ ] Rate limiting enabled
- [ ] Application Insights configured
- [ ] Automated backups enabled
- [ ] Disaster recovery plan

---

**Deployment Complete!** 🎉

Your ReviewHub application is now live in production.
