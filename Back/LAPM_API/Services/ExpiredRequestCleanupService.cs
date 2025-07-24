using Microsoft.EntityFrameworkCore;

namespace LAPM_API.Services
{
    public class ExpiredRequestCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredRequestCleanupService> _logger;

        public ExpiredRequestCleanupService(IServiceProvider serviceProvider, ILogger<ExpiredRequestCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Expired Request Cleanup Service is starting.");

            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkAsync();
            }
        }

        private async Task DoWorkAsync()
        {
            _logger.LogInformation("Expired Request Cleanup Service is running.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();

                var now = DateTime.UtcNow;

                // Find all requests that are still marked as active but are past their expiration time.
                var expiredRequests = await dbContext.AccessRequests
                    .Where(r => (r.Status == Models.RequestStatus.Approved || r.Status == Models.RequestStatus.Applied)
                               && r.ExpirationTime < now)
                    .ToListAsync();

                if (expiredRequests.Any())
                {
                    _logger.LogInformation($"Found {expiredRequests.Count} expired requests to update.");
                    foreach (var request in expiredRequests)
                    {
                        request.Status = Models.RequestStatus.Expired;
                    }

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated expired requests.");
                }
                else
                {
                    _logger.LogInformation("No expired requests found to update.");
                }
            }
        }
    }
}
