using Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using WebAppCellMapper.Data.Repositories;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    public class StationsScanningManager : IStationsScanningManager
    {
        private readonly IServiceProvider sp;
        private readonly ILogger<StationsScanningManager> logger;

        private QueryResult result { get; set;}
        private Task? task=null;
        private CancellationTokenSource? TokenSourceTask = null;
        private readonly object _lock=new object();

        public StationsScanningManager(IServiceProvider serviceProvider,ILogger<StationsScanningManager> logger)
        {
            sp = serviceProvider;
            this.logger = logger;
            result = new QueryResult(string.Empty, Data.Models.NetworkStandard.Gsm, 0, 0, 0, "задачи нет", true);
        }


        public void StartFullScan()
        {
            if (task != null) return;
            TokenSourceTask = new CancellationTokenSource();

            task = Task.Run(async () =>
            {
                await FullScan(TokenSourceTask.Token);
            });
        }
        private async Task FullScan(CancellationToken ct = default)
        {
            try
            {
                using var scope = sp.CreateScope();
                var stationsService = scope.ServiceProvider.GetRequiredService<IStationsService>();
                logger.LogInformation($"FullScan start");
                try
                {


                    await foreach (var item in stationsService.SyncStationsAllAsync(ct))
                    {
                        UpdateResult(item);
                        logger.LogInformation($"OperatorCode={item.OperatorCode}, Network={item.Network}, CountAdded={item.CountAdded}, " +
                            $"CountSectorsScaned={item.CountSectorsScaned}, CountSectors={item.CountSectors}, isDone={item.isDone}");
                    }
                }
                catch (Exception ex)
                {

                    logger.LogError(ex.Message);
                }

            }
            catch (OperationCanceledException)
            {

                logger.LogError("OperationCanceledException FullScan");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during FullScan");
            }
            finally
            {
                if (TokenSourceTask != null)
                {
                    TokenSourceTask.Dispose();
                    TokenSourceTask = null;
                }
                task = null;
            }

        }
        private void UpdateResult(QueryResult newResult)
        {
            result = newResult;
        }
        public QueryResult GetCurrentProcess()
        {
            return result;
            
        }
        public async Task StopCurrentProccess()
        {
            Task? locT=null; 
            lock (_lock)
            {
                locT = task;
                if (TokenSourceTask != null)
                {
                    TokenSourceTask.Cancel();

                }
            }
            if (locT != null)
            {
                await locT;
            }
        }
        public async Task<int> CanceledProccess()
        {

            try
            {
                await StopCurrentProccess();
                using var scope = sp.CreateScope();
                var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();
                return await progress.FailedProgress();


            }
            catch (OperationCanceledException)
            {

                logger.LogError("OperationCanceledException FullScan");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error CanceledProccess");
            }
            return 0;
        }
    }
}
