# ReviewHub Production Deployment Checklist

## Pre-Deployment Checklist

### 1. Environment Configuration
- [ ] Create `.env.production` from `.env.production.example`
- [ ] Configure all API keys and secrets:
  - [ ] `DB_SA_PASSWORD` - SQL Server SA password
  - [ ] `AUTH0_DOMAIN` - Auth0 tenant domain
  - [ ] `AUTH0_AUDIENCE` - Auth0 API audience
  - [ ] `AUTH0_CLIENT_ID` - Auth0 application client ID
  - [ ] `AUTH0_CLIENT_SECRET` - Auth0 application client secret
  - [ ] `STRIPE_SECRET_KEY` - Stripe secret key
  - [ ] `STRIPE_PUBLISHABLE_KEY` - Stripe publishable key
  - [ ] `STRIPE_WEBHOOK_SECRET` - Stripe webhook secret
  - [ ] `TWILIO_ACCOUNT_SID` - Twilio account SID
  - [ ] `TWILIO_AUTH_TOKEN` - Twilio auth token
  - [ ] `TWILIO_PHONE_NUMBER` - Twilio phone number
  - [ ] `MAILGUN_API_KEY` - Mailgun API key
  - [ ] `MAILGUN_DOMAIN` - Mailgun domain
  - [ ] `MAILGUN_FROM_EMAIL` - Mailgun sender email
  - [ ] `GOOGLE_CLIENT_ID` - Google OAuth client ID
  - [ ] `GOOGLE_CLIENT_SECRET` - Google OAuth client secret
  - [ ] `GOOGLE_PLACES_API_KEY` - Google Places API key
  - [ ] `OPENAI_API_KEY` - OpenAI API key
  - [ ] `YELP_API_KEY` - Yelp Fusion API key
  - [ ] `FACEBOOK_APP_ID` - Facebook app ID
  - [ ] `FACEBOOK_APP_SECRET` - Facebook app secret
  - [ ] `APP_URL` - Frontend application URL
  - [ ] `API_URL` - Backend API URL

### 2. Auth0 Configuration
- [ ] Enable Multi-Factor Authentication (MFA) in Auth0 dashboard
- [ ] Set MFA policy to "Always" required
- [ ] Configure MFA methods (SMS + Authenticator App)
- [ ] Set up email verification templates
- [ ] Configure password policy:
  - [ ] Minimum 8 characters
  - [ ] Require uppercase letters
  - [ ] Require lowercase letters
  - [ ] Require numbers
  - [ ] Require special characters
- [ ] Create Auth0 Management API application
- [ ] Configure callback URLs for production domain
- [ ] Configure logout URLs for production domain
- [ ] Configure allowed web origins

### 3. Database Setup
- [ ] Set up production SQL Server instance
- [ ] Configure SQL Server firewall rules
- [ ] Create production database
- [ ] Verify database connection string
- [ ] Review and test database backup strategy
- [ ] Configure database maintenance plans

### 4. Docker & Infrastructure
- [ ] Install Docker on production server
- [ ] Install Docker Compose on production server
- [ ] Verify Docker daemon is running
- [ ] Configure Docker security settings
- [ ] Set up Docker volume backups
- [ ] Configure Docker log rotation

### 5. Domain & SSL
- [ ] Purchase/configure production domain
- [ ] Set up DNS records (A/AAAA records)
- [ ] Obtain SSL certificate (Let's Encrypt recommended)
- [ ] Configure reverse proxy (Nginx/Caddy) for HTTPS
- [ ] Update APP_URL and API_URL in .env.production
- [ ] Update VITE_API_URL in CI/CD secrets

### 6. GitHub Secrets Configuration
Required GitHub Secrets for CI/CD pipeline:
- [ ] `DOCKER_USERNAME` - Docker Hub username
- [ ] `DOCKER_PASSWORD` - Docker Hub password/token
- [ ] `VITE_API_URL` - Production API URL
- [ ] `VITE_AUTH0_DOMAIN` - Auth0 domain
- [ ] `VITE_AUTH0_CLIENT_ID` - Auth0 client ID
- [ ] `VITE_AUTH0_AUDIENCE` - Auth0 audience
- [ ] `STAGING_API_URL` - Staging API URL
- [ ] `PRODUCTION_HOST` - Production server hostname/IP
- [ ] `PRODUCTION_USERNAME` - SSH username for production
- [ ] `PRODUCTION_SSH_KEY` - SSH private key for production
- [ ] `STAGING_HOST` - Staging server hostname/IP
- [ ] `STAGING_USERNAME` - SSH username for staging
- [ ] `STAGING_SSH_KEY` - SSH private key for staging

### 7. Server Setup
- [ ] Provision production server (Ubuntu 20.04+ recommended)
- [ ] Configure server firewall (UFW):
  - [ ] Allow SSH (port 22)
  - [ ] Allow HTTP (port 80)
  - [ ] Allow HTTPS (port 443)
- [ ] Set up SSH key authentication
- [ ] Disable password authentication
- [ ] Configure fail2ban for SSH protection
- [ ] Set up automated security updates
- [ ] Create deployment directory: `/opt/reviewhub`
- [ ] Clone repository to deployment directory
- [ ] Set execute permissions on deploy.sh: `chmod +x deploy.sh`

### 8. Third-Party Service Setup
- [ ] **Stripe**: Create production webhook endpoint
- [ ] **Twilio**: Verify phone numbers and SMS capabilities
- [ ] **Mailgun**: Configure and verify domain for sending
- [ ] **Google APIs**: Enable required APIs (Places, OAuth)
- [ ] **OpenAI**: Verify API key and usage limits
- [ ] **Yelp**: Verify API key and rate limits
- [ ] **Facebook**: Configure app for production use

### 9. Monitoring & Logging
- [ ] Set up application monitoring (e.g., Application Insights, DataDog)
- [ ] Configure error tracking (e.g., Sentry)
- [ ] Set up uptime monitoring (e.g., UptimeRobot)
- [ ] Configure log aggregation (e.g., ELK stack, Loki)
- [ ] Set up alerts for critical errors
- [ ] Configure performance monitoring
- [ ] Set up database monitoring

### 10. Security Hardening
- [ ] Review and update CORS settings in API
- [ ] Configure rate limiting in API
- [ ] Enable HTTPS-only mode
- [ ] Configure security headers (already in nginx.conf)
- [ ] Review and harden Content Security Policy
- [ ] Set up Web Application Firewall (WAF)
- [ ] Configure DDoS protection (Cloudflare recommended)
- [ ] Review and secure all API endpoints
- [ ] Implement IP whitelisting where appropriate

## Deployment Process

### Initial Deployment
1. [ ] Verify all pre-deployment checklist items are complete
2. [ ] Test build locally: `docker-compose build`
3. [ ] Push code to `main` branch
4. [ ] Monitor GitHub Actions workflow
5. [ ] Verify Docker images are pushed to Docker Hub
6. [ ] Monitor deployment to production server
7. [ ] Verify health checks pass
8. [ ] Test application functionality

### Manual Deployment (if needed)
```bash
# SSH into production server
ssh username@production-host

# Navigate to deployment directory
cd /opt/reviewhub

# Pull latest changes
git pull origin main

# Run deployment script
chmod +x deploy.sh
./deploy.sh
```

### Database Migration
- [ ] Backup production database before migration
- [ ] Run migrations: `docker-compose exec -T api dotnet ef database update`
- [ ] Verify migration success
- [ ] Test application with new schema

## Post-Deployment Verification

### Health Checks
- [ ] API health endpoint: `curl http://your-domain.com/health`
- [ ] Client health endpoint: `curl http://your-domain.com/health`
- [ ] Database connectivity test
- [ ] Auth0 authentication test
- [ ] Payment processing test (Stripe)
- [ ] SMS notifications test (Twilio)
- [ ] Email delivery test (Mailgun)

### Functional Testing
- [ ] User registration and MFA enrollment
- [ ] User login with MFA
- [ ] Team invitation flow
- [ ] Business profile management
- [ ] Review management
- [ ] Analytics dashboard
- [ ] Competitor analysis
- [ ] AI features (response generation, sentiment analysis)
- [ ] Subscription/payment flow
- [ ] Multi-location support

### Performance Testing
- [ ] Load test API endpoints
- [ ] Measure page load times
- [ ] Check database query performance
- [ ] Verify CDN/caching is working
- [ ] Test concurrent user capacity

### Security Testing
- [ ] Run security scan (OWASP ZAP)
- [ ] Test authentication/authorization
- [ ] Verify HTTPS is enforced
- [ ] Test CORS configuration
- [ ] Verify sensitive data is not exposed
- [ ] Check for SQL injection vulnerabilities
- [ ] Test XSS protection

## Ongoing Maintenance

### Daily Tasks
- [ ] Monitor error logs
- [ ] Check system health dashboards
- [ ] Review uptime monitoring alerts

### Weekly Tasks
- [ ] Review application performance metrics
- [ ] Check database performance and growth
- [ ] Review security alerts
- [ ] Update dependencies if needed

### Monthly Tasks
- [ ] Review and rotate API keys/secrets
- [ ] Database backup verification and testing
- [ ] Security patch updates
- [ ] Performance optimization review
- [ ] Cost optimization review

## Rollback Procedure

If deployment fails or critical issues arise:

1. [ ] Stop current containers: `docker-compose down`
2. [ ] Checkout previous stable version: `git checkout <previous-tag>`
3. [ ] Run deployment: `./deploy.sh`
4. [ ] Restore database backup if needed
5. [ ] Verify application is functioning
6. [ ] Investigate and fix issues
7. [ ] Create new deployment when ready

## Emergency Contacts

- **DevOps Lead**: [Name/Contact]
- **Database Admin**: [Name/Contact]
- **Security Team**: [Name/Contact]
- **Auth0 Support**: support@auth0.com
- **Stripe Support**: support@stripe.com
- **Twilio Support**: support@twilio.com
- **Mailgun Support**: support@mailgun.com

## Useful Commands

```bash
# View logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f api
docker-compose logs -f client
docker-compose logs -f database

# Restart services
docker-compose restart api
docker-compose restart client

# Stop all services
docker-compose down

# Start all services
docker-compose up -d

# Check running containers
docker-compose ps

# Execute commands in containers
docker-compose exec api dotnet ef migrations list
docker-compose exec database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <password>

# Database backup
docker-compose exec database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <password> -Q "BACKUP DATABASE ReviewHubDb TO DISK='/var/opt/mssql/backup/ReviewHubDb.bak'"

# Database restore
docker-compose exec database /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <password> -Q "RESTORE DATABASE ReviewHubDb FROM DISK='/var/opt/mssql/backup/ReviewHubDb.bak' WITH REPLACE"
```

## Resources

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Deployment Guide](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [Auth0 Documentation](https://auth0.com/docs)
- [Nginx Documentation](https://nginx.org/en/docs/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Let's Encrypt Documentation](https://letsencrypt.org/docs/)
