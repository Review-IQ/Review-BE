# ReviewHub Monitoring & Observability Guide

## Health Check Endpoints

### API Health Checks

The API provides the following health check endpoints:

#### 1. General Health Check
- **URL**: `GET /health`
- **Description**: Basic health check for API availability
- **Returns**: `200 OK` (Healthy) or `503 Service Unavailable` (Unhealthy)
- **Checks**: Database connectivity

```bash
curl http://localhost:5000/health
```

#### 2. Readiness Check
- **URL**: `GET /health/ready`
- **Description**: Checks if the API is ready to serve requests
- **Returns**: `200 OK` (Ready) or `503 Service Unavailable` (Not Ready)
- **Checks**: Services tagged with "ready"

```bash
curl http://localhost:5000/health/ready
```

### Client Health Check

The React client provides a health check endpoint configured in Nginx:

- **URL**: `GET /health`
- **Returns**: `200 OK` with "healthy" text

```bash
curl http://localhost:3000/health
```

## Application Monitoring

### Recommended Monitoring Stack

#### 1. Application Performance Monitoring (APM)

**Option A: Application Insights (Azure)**
```csharp
// Add to Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});
```

**Option B: DataDog**
```csharp
// Add to Program.cs
builder.Services.AddDatadogTracing();
builder.Services.AddDatadogMetrics();
```

#### 2. Error Tracking

**Sentry Integration**

Install package:
```bash
dotnet add package Sentry.AspNetCore
```

Add to Program.cs:
```csharp
builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.Environment = builder.Environment.EnvironmentName;
    options.TracesSampleRate = 1.0;
});
```

#### 3. Structured Logging

**Serilog Configuration**

Install packages:
```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Seq
```

Add to Program.cs:
```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/reviewhub-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();
```

appsettings.Production.json:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    }
  },
  "Seq": {
    "ServerUrl": "${SEQ_SERVER_URL}"
  }
}
```

### 4. Uptime Monitoring

**UptimeRobot** (Recommended - Free tier available)
- Monitor API health endpoint every 5 minutes
- Configure alerts via email/SMS/Slack
- Track response time and uptime percentage

**Pingdom** (Alternative)
- More detailed performance monitoring
- Global monitoring locations
- Advanced alerting

Configuration:
1. Create monitor for: `https://api.yourdomain.com/health`
2. Check interval: 5 minutes
3. Alert after: 2 consecutive failures
4. Contact methods: Email, SMS, Slack

## Metrics to Monitor

### 1. API Metrics

**Request Metrics**:
- Request rate (requests/second)
- Response time (p50, p95, p99)
- Error rate (5xx errors)
- Status code distribution

**Database Metrics**:
- Connection pool usage
- Query duration
- Failed connections
- Active connections

**External Service Metrics**:
- Auth0 authentication latency
- Stripe API response time
- Twilio SMS delivery rate
- Mailgun email delivery rate
- Google Places API response time
- OpenAI API response time

### 2. Infrastructure Metrics

**Container Metrics** (Docker):
```bash
# View resource usage
docker stats

# Specific container
docker stats reviewhub-api
```

**System Metrics**:
- CPU usage (target: <70%)
- Memory usage (target: <80%)
- Disk I/O
- Network I/O
- Disk space (alert at 80% full)

**Database Metrics**:
- CPU usage
- Memory usage
- Connection count
- Query performance
- Index usage
- Deadlocks

### 3. Business Metrics

**User Activity**:
- Daily active users
- User registrations
- Login success/failure rate
- MFA enrollment rate

**Review Management**:
- Reviews synced per day
- Review response rate
- Average response time
- AI response generation usage

**Subscription Metrics**:
- Trial signups
- Conversions
- Churn rate
- MRR (Monthly Recurring Revenue)

## Alerting Strategy

### Critical Alerts (Immediate Response - 24/7)

1. **API Down**: Health check fails for 2+ consecutive checks
2. **Database Connection Failure**: Cannot connect to database
3. **High Error Rate**: >5% of requests returning 5xx errors
4. **Payment Processing Failure**: Stripe webhook errors
5. **Security Alert**: Multiple failed login attempts, suspicious activity

### Warning Alerts (Response within 1 hour)

1. **High Response Time**: p95 latency > 2 seconds
2. **Low Disk Space**: <20% disk space remaining
3. **Memory Usage**: >80% memory utilization
4. **External Service Degradation**: Slow response from third-party APIs
5. **Background Job Failures**: Polling services failing

### Informational Alerts (Review next business day)

1. **Resource Usage Trends**: Gradual increase in resource usage
2. **Unusual Traffic Patterns**: Significant deviation from baseline
3. **Feature Usage**: Low usage of new features

## Log Aggregation

### ELK Stack Setup (Elasticsearch, Logstash, Kibana)

docker-compose addition:
```yaml
elasticsearch:
  image: docker.elastic.co/elasticsearch/elasticsearch:8.10.0
  environment:
    - discovery.type=single-node
  ports:
    - "9200:9200"
  volumes:
    - elasticsearch-data:/usr/share/elasticsearch/data

kibana:
  image: docker.elastic.co/kibana/kibana:8.10.0
  ports:
    - "5601:5601"
  environment:
    - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
  depends_on:
    - elasticsearch

logstash:
  image: docker.elastic.co/logstash/logstash:8.10.0
  volumes:
    - ./logstash/pipeline:/usr/share/logstash/pipeline
  depends_on:
    - elasticsearch
```

### Grafana Loki (Lightweight Alternative)

```yaml
loki:
  image: grafana/loki:2.9.0
  ports:
    - "3100:3100"
  volumes:
    - loki-data:/loki

grafana:
  image: grafana/grafana:10.1.0
  ports:
    - "3001:3000"
  environment:
    - GF_SECURITY_ADMIN_PASSWORD=admin
  volumes:
    - grafana-data:/var/lib/grafana
```

## Dashboard Setup

### Grafana Dashboards

**API Performance Dashboard**:
- Request rate graph
- Response time percentiles (p50, p95, p99)
- Error rate by endpoint
- Status code distribution
- Database query performance

**Infrastructure Dashboard**:
- CPU usage per container
- Memory usage per container
- Network I/O
- Disk usage
- Container restart count

**Business Metrics Dashboard**:
- User signups
- Active users
- Reviews processed
- Revenue metrics
- Feature usage

### Example Prometheus Configuration

```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'reviewhub-api'
    static_configs:
      - targets: ['api:80']

  - job_name: 'node-exporter'
    static_configs:
      - targets: ['node-exporter:9100']

  - job_name: 'sql-server-exporter'
    static_configs:
      - targets: ['sql-exporter:9399']
```

## Custom Health Checks

### External Service Health Checks

Create custom health checks for external services:

```csharp
// Auth0HealthCheck.cs
public class Auth0HealthCheck : IHealthCheck
{
    private readonly IAuth0Service _auth0Service;

    public Auth0HealthCheck(IAuth0Service auth0Service)
    {
        _auth0Service = auth0Service;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Test Auth0 connectivity
            await _auth0Service.GetManagementApiClientAsync();
            return HealthCheckResult.Healthy("Auth0 is accessible");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Auth0 is not accessible", ex);
        }
    }
}

// Register in Program.cs
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database")
    .AddCheck<Auth0HealthCheck>("auth0", tags: new[] { "external" });
```

### Database-Specific Health Checks

```csharp
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
            stopwatch.Stop();

            var data = new Dictionary<string, object>
            {
                { "latency", stopwatch.ElapsedMilliseconds }
            };

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                return HealthCheckResult.Degraded("Database is slow", data: data);
            }

            return HealthCheckResult.Healthy("Database is healthy", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database is unhealthy", ex);
        }
    }
}
```

## Performance Baselines

### Expected Performance Metrics (Production)

**API Response Times** (95th percentile):
- GET requests: < 200ms
- POST requests: < 500ms
- AI operations: < 3s
- Database queries: < 100ms

**Availability Targets**:
- API: 99.9% uptime (< 43 minutes downtime/month)
- Database: 99.95% uptime
- Background jobs: 99% success rate

**Resource Utilization** (Normal Load):
- API CPU: 30-50%
- API Memory: 40-60%
- Database CPU: 20-40%
- Database Memory: 50-70%

## Incident Response

### Incident Severity Levels

**P0 - Critical** (Response: Immediate):
- Complete service outage
- Data loss or corruption
- Security breach
- Payment processing completely down

**P1 - High** (Response: 30 minutes):
- Partial service outage
- Major feature not working
- Performance severely degraded
- External service integration down

**P2 - Medium** (Response: 2 hours):
- Minor feature not working
- Performance degraded for some users
- Workaround available

**P3 - Low** (Response: Next business day):
- Cosmetic issues
- Minor bugs with minimal impact
- Feature requests

### Incident Response Checklist

1. **Acknowledge**:
   - Acknowledge alert immediately
   - Notify team via Slack/PagerDuty

2. **Assess**:
   - Check health dashboards
   - Review recent logs
   - Identify affected users

3. **Mitigate**:
   - Apply immediate fix or rollback
   - Update status page
   - Communicate with users

4. **Resolve**:
   - Verify fix is working
   - Monitor for 30 minutes
   - Update incident record

5. **Post-Mortem**:
   - Root cause analysis
   - Action items to prevent recurrence
   - Documentation update

## Monitoring Tools Reference

### Free/Open Source
- **Grafana**: Dashboards and visualization
- **Prometheus**: Metrics collection
- **Loki**: Log aggregation
- **UptimeRobot**: Uptime monitoring (free tier)
- **Seq**: Structured log viewer (free single-user)

### Paid/Commercial
- **DataDog**: All-in-one observability ($15/host/month)
- **New Relic**: APM and monitoring ($25/user/month)
- **Application Insights**: Azure native ($0.25/GB ingested)
- **Sentry**: Error tracking ($26/month)
- **PagerDuty**: Incident management ($21/user/month)

### Recommended Starter Stack (Cost-Effective)
1. **UptimeRobot** (Free) - Uptime monitoring
2. **Sentry** ($26/month) - Error tracking
3. **Grafana Cloud** (Free tier) - Metrics and dashboards
4. **Docker logs** + **Seq** (Free) - Log aggregation
5. **GitHub Actions** (Free) - CI/CD monitoring

**Total Monthly Cost**: ~$26/month

## Custom Metrics Implementation

### Adding Custom Metrics

```csharp
// Install package
// dotnet add package prometheus-net.AspNetCore

// Program.cs
using Prometheus;

// Enable metrics endpoint
app.UseMetricServer(); // /metrics endpoint
app.UseHttpMetrics(); // HTTP metrics

// Custom metrics example
public class ReviewMetrics
{
    private static readonly Counter ReviewsProcessed = Metrics
        .CreateCounter("reviews_processed_total", "Total number of reviews processed");

    private static readonly Histogram ReviewProcessingDuration = Metrics
        .CreateHistogram("review_processing_duration_seconds", "Review processing duration");

    private static readonly Gauge ActiveReviewSyncs = Metrics
        .CreateGauge("active_review_syncs", "Number of active review sync operations");

    public static void IncrementReviewsProcessed() => ReviewsProcessed.Inc();

    public static IDisposable TrackProcessingTime() => ReviewProcessingDuration.NewTimer();

    public static void SetActiveSyncs(int count) => ActiveReviewSyncs.Set(count);
}

// Usage in service
using (ReviewMetrics.TrackProcessingTime())
{
    // Process review
    await ProcessReviewAsync(review);
    ReviewMetrics.IncrementReviewsProcessed();
}
```

## Monitoring Checklist

### Daily
- [ ] Review error logs
- [ ] Check uptime status
- [ ] Verify backup success

### Weekly
- [ ] Review performance metrics
- [ ] Check resource utilization trends
- [ ] Review security alerts
- [ ] Test alert notifications

### Monthly
- [ ] Review and update dashboards
- [ ] Analyze incident patterns
- [ ] Capacity planning review
- [ ] Cost optimization review
- [ ] Update runbooks

### Quarterly
- [ ] Disaster recovery drill
- [ ] Review and update monitoring strategy
- [ ] Audit alert rules
- [ ] Performance benchmark review
