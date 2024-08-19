using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProjectOrigin.Chronicler.Server.Options;

namespace ProjectOrigin.Chronicler.Server.BlockReader;

public class BlockReaderBackgroundService : BackgroundService
{
    private readonly ILogger<BlockReaderBackgroundService> _logger;
    private readonly ChroniclerOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public BlockReaderBackgroundService(
        ILogger<BlockReaderBackgroundService> logger,
        IOptions<ChroniclerOptions> options,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.JobInterval);

        do
        {
            try
            {
                _logger.LogTrace("Executing BlockReader");

                var job = _serviceProvider.GetRequiredService<IBlockReaderJob>();
                await job.ProcessAllRegistries(stoppingToken);

                _logger.LogTrace("Executed BlockReader");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Error executing BlockReader");
            }
        }
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken));
    }
}
