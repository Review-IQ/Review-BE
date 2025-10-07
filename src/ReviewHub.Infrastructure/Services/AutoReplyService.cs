using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReviewHub.Infrastructure.Data;

namespace ReviewHub.Infrastructure.Services;

public class AutoReplyService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoReplyService> _logger;

    public AutoReplyService(IServiceProvider serviceProvider, ILogger<AutoReplyService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Reply Service started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingReviewsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Auto-Reply Service");
                }

                // Check every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            // Expected when the application is shutting down
            _logger.LogInformation("Auto-Reply Service is stopping");
        }
    }

    private async Task ProcessPendingReviewsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var aiService = scope.ServiceProvider.GetRequiredService<IAIService>();

        // Get reviews without responses that were created in the last 24 hours
        var cutoffDate = DateTime.UtcNow.AddHours(-24);
        var pendingReviews = await context.Reviews
            .Include(r => r.Business)
                .ThenInclude(b => b.User)
            .Where(r => r.ResponseText == null && r.CreatedAt >= cutoffDate)
            .ToListAsync();

        foreach (var review in pendingReviews)
        {
            try
            {
                // Get AI settings for the business owner
                var settings = await context.AISettings
                    .FirstOrDefaultAsync(s => s.UserId == review.Business.UserId);

                if (settings == null || !await aiService.ShouldAutoReplyAsync(review, settings))
                {
                    continue;
                }

                // Generate and save response
                var response = await aiService.GenerateReviewResponseAsync(
                    review,
                    review.Business,
                    settings.ResponseTone,
                    settings.ResponseLength
                );

                review.ResponseText = response;
                review.ResponseDate = DateTime.UtcNow;
                review.IsAutoReplied = true;

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    "Auto-replied to review {ReviewId} for business {BusinessName}",
                    review.Id,
                    review.Business.Name
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-replying to review {ReviewId}", review.Id);
            }
        }
    }
}
