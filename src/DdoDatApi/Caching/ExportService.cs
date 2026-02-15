using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace DdoDatApi.Caching;

public abstract class ExportService : BackgroundService
{
    private readonly SemaphoreSlim _signal = new SemaphoreSlim(0); // Used to signal a new task is ready

    public void Export()
    {
        _signal.Release();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _signal.WaitAsync(stoppingToken); // Wait for a signal from the controller
            await Export(stoppingToken);
        }
    }

    public abstract Task Export(CancellationToken token);
}
