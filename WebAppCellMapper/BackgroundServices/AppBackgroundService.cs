
using Microsoft.Extensions.DependencyInjection;
using System;
using WebAppCellMapper.Data.Repositories;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.BackgroundServices
{
    public class AppBackgroundService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IStationsScanningManager scanningManager;
        private readonly ILogger<AppBackgroundService> logger;

        public AppBackgroundService(IServiceProvider serviceProvider, IStationsScanningManager scanningManager, ILogger<AppBackgroundService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.scanningManager = scanningManager;
            this.logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            try
            {
                await Task.WhenAll(
                    AppBackgroundAsync(stoppingToken), //авто старт сканирования карты 4cells
                    EraserProgressGrabberAsync(stoppingToken), //очистка прогресса
                    EraserTracePointsAsync(stoppingToken) //очистка старых точек локатора
                );

            }
            catch (OperationCanceledException)
            {
                logger.LogError("AppBackgroundService was canceled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex,$"error ex = {ex.Message}");
            }

        }
        private  async Task EraserProgressGrabberAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<IProgressRepository>();
                    await service.DeleteCompletedProgress(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
        private async Task EraserTracePointsAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var service = scope.ServiceProvider.GetRequiredService<ILocationRepository>();
                    await service.EraseOldLocation(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
        private async Task AppBackgroundAsync(CancellationToken stoppingToken)
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
