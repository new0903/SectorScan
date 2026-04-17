
using WebAppCellMapper.Services;

namespace WebAppCellMapper.BackgroundServices
{
    public class AppBackgroundService : BackgroundService
    {
        private readonly IStationsScanningManager scanningManager;
        private readonly ILogger<AppBackgroundService> logger;

        public AppBackgroundService( IStationsScanningManager scanningManager,ILogger<AppBackgroundService> logger)
        {
            this.scanningManager = scanningManager;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested) 
            {
                if (!scanningManager.IsWorking)
                {
                    scanningManager.StartFullScan(true);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
