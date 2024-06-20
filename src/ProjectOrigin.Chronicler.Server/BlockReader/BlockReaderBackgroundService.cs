using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectOrigin.ServiceCommon.Database;

namespace ProjectOrigin.Chronicler.Server.Repositories;

public class BlockReaderBackgroundService : BackgroundService
{
    private readonly TimeSpan _jobInterval = TimeSpan.FromHours(1);
    private readonly ILogger<BlockReaderBackgroundService> _logger;
    private readonly ServiceProvider _serviceProvider;

    public BlockReaderBackgroundService(ILogger<BlockReaderBackgroundService> logger, ServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_jobInterval);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _logger.LogTrace("Executing BlockReader");

                var job = _serviceProvider.GetRequiredService<BlockReaderJob>();
                await job.Process(stoppingToken);

                _logger.LogTrace("Executed BlockReader");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error executing BlockReader");
            }
        }
    }
}
