using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ReviewHub.Infrastructure.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("ReviewHub.API")));

// Auth0 Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// External Services
builder.Services.AddHttpClient();

// Register Twilio SMS Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.ISmsService, ReviewHub.Infrastructure.Services.TwilioSmsService>();

// Register Email Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.IEmailService, ReviewHub.Infrastructure.Services.MailgunEmailService>();

// Register Auth0 Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.IAuth0Service, ReviewHub.Infrastructure.Services.Auth0Service>();

// Register Platform OAuth Services
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.IGoogleBusinessService, ReviewHub.Infrastructure.Services.GoogleBusinessService>();
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.IYelpService, ReviewHub.Infrastructure.Services.YelpService>();
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.IFacebookService, ReviewHub.Infrastructure.Services.FacebookService>();

// Register Notification Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.INotificationService, ReviewHub.Infrastructure.Services.NotificationService>();

// Register Team Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.ITeamService, ReviewHub.Infrastructure.Services.TeamService>();

// Register AI Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.IAIService, ReviewHub.Infrastructure.Services.OpenAIService>();

// Register Competitor Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.ICompetitorService, ReviewHub.Infrastructure.Services.GooglePlacesCompetitorService>();

// Register Location Service
builder.Services.AddScoped<ReviewHub.Infrastructure.Services.ILocationService, ReviewHub.Infrastructure.Services.LocationService>();

// Register Background Services
builder.Services.AddHostedService<ReviewHub.Infrastructure.Services.YelpPollingService>();
builder.Services.AddHostedService<ReviewHub.Infrastructure.Services.AutoReplyService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
// app.UseHttpsRedirection(); // Commented out for local dev - can cause CORS issues

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check Endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
