
using Microsoft.Extensions.DependencyInjection;
using System;
using WebAppCellMapper.Data.Repositories;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.BackgroundServices
{
    public class EraserDbBackgroundService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger<EraserDbBackgroundService> logger;

        public EraserDbBackgroundService(IServiceProvider serviceProvider, ILogger<EraserDbBackgroundService> logger)
        {
            this.serviceProvider = serviceProvider;
            this.logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
               using( var scope = serviceProvider.CreateScope())
               {
                    var service=scope.ServiceProvider.GetRequiredService<IProgressRepository>();
                    await service.DeleteCompletedProgress(stoppingToken);
               }
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}
