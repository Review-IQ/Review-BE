using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReviewHub.Core.Enums;
using ReviewHub.Infrastructure.Data;

namespace ReviewHub.Infrastructure.Services;

public class YelpPollingService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<YelpPollingService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(15); // Poll every 15 minutes

    public YelpPollingService(
        IServiceProvider services,
        ILogger<YelpPollingService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Yelp Polling Service is starting");

        try
        {
            // Wait 30 seconds before starting the first poll
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Yelp Polling Service is checking for new reviews");

                    using var scope = _services.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var yelpService = scope.ServiceProvider.GetRequiredService<IYelpService>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                    // Get all active Yelp connections
                    var yelpConnections = await context.PlatformConnections
                        .Where(c => c.Platform == ReviewPlatform.Yelp && c.IsActive && c.AutoSync)
                        .ToListAsync(stoppingToken);

                    if (yelpConnections.Any())
                    {
                        _logger.LogInformation("Found {Count} active Yelp connections to sync", yelpConnections.Count);

                        foreach (var connection in yelpConnections)
                        {
                            try
                            {
                                // Fetch reviews
                                var newReviews = await yelpService.FetchReviewsAsync(connection.Id);

                                if (newReviews.Any())
                                {
                                    _logger.LogInformation("Found {Count} new Yelp reviews for business {BusinessId}",
                                        newReviews.Count, connection.BusinessId);

                                    // Send notification for each new review
                                    foreach (var review in newReviews)
                                    {
                                        await notificationService.SendReviewNotificationAsync(
                                            connection.BusinessId,
                                            "Yelp",
                                            review);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error polling Yelp reviews for connection {ConnectionId}", connection.Id);
                                // Continue with other connections
                            }
                        }
                    }

                    _logger.LogInformation("Yelp Polling Service completed. Next poll in {Minutes} minutes",
                        _pollingInterval.TotalMinutes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Yelp polling service");
                }

                // Wait for the next poll
                await Task.Delay(_pollingInterval, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            // Expected when the application is shutting down
            _logger.LogInformation("Yelp Polling Service is stopping",ex);
        }
    }
}
