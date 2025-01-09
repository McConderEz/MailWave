using MailWave.Mail.Infrastructure.Dispatchers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MailWave.Mail.Infrastructure.BackgroundServices;

/// <summary>
/// Фоновый процесс для очистки неактивных клиентов(неактивными считаются те, что не совершали операции более 15 минут)
/// </summary>
public class CleanupInactiveClientsBackgroundService: BackgroundService
{
    private const int FREQUENCY_OF_REVISION = 15;
    private readonly ILogger<CleanupInactiveClientsBackgroundService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CleanupInactiveClientsBackgroundService(
        ILogger<CleanupInactiveClientsBackgroundService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CleanupInactiveClientsBackgroundService is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();

            var dispatcher = scope.ServiceProvider.GetRequiredService<MailClientDispatcher>();

            _logger.LogInformation("CleanupInactiveClientsBackgroundService is working");
            
            await dispatcher.CleanupInactiveClientsAsync(stoppingToken);
            
            _logger.LogInformation("CleanupInactiveClientsBackgroundService is stopped");

            await Task.Delay(TimeSpan.FromMinutes(FREQUENCY_OF_REVISION), stoppingToken);
        }

        await Task.CompletedTask;
    }
}