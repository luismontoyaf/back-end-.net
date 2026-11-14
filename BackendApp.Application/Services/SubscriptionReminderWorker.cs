using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public sealed class SubscriptionReminderWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionReminderWorker> _logger;

        public SubscriptionReminderWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionReminderWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var subscriptionService = scope.ServiceProvider.GetRequiredService<SubscriptionService>();
                    await subscriptionService.ProcessDueRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Subscription reminder processing failed.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }
}
