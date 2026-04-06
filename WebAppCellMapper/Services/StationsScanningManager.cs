using Azure;
using Grpc.Core.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using WebAppCellMapper.Data;
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

        public bool IsWorking => task != null;

        public QueryResult GetCurrentProcess => result;

        public void StartFullScan(bool isAutoStart)
        {
            lock (_lock)
            {
                if (task != null) return;
                TokenSourceTask = new CancellationTokenSource();

                task = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = sp.CreateScope();
                        var runtime = scope.ServiceProvider.GetRequiredService<IRuntimeRepository>();

                        if (await runtime.IsRunning()|| !isAutoStart)
                        {
                            var stationsService = scope.ServiceProvider.GetRequiredService<IStationsService>();
                            await runtime.StartRuntime();
                            await FullScan(stationsService, TokenSourceTask.Token);
                            await runtime.CancelRuntime(TokenSourceTask.Token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogInformation("OperationCanceledException");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError( ex, ex.Message);
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

                });
            }
        }







        private async Task FullScan(IStationsService stationsService, CancellationToken ct = default)
        {
            logger.LogInformation($"FullScan start");
            try
            {


                await foreach (var item in stationsService.SyncStationsAllAsync(ct))
                {
                    result = item;
                    logger.LogInformation($"OperatorCode={item.OperatorCode}, Network={item.Network}, CountAdded={item.CountAdded}, " +
                        $"CountSectorsScaned={item.CountSectorsScaned}, CountSectors={item.CountSectors}, isDone={item.isDone}");
                }
            }
            catch (OperationCanceledException)
            {

                logger.LogError("OperationCanceledException FullScan");
            }
            catch (Exception ex)
            {

                logger.LogError(ex, ex.Message);
            }

            

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
            {
                using var scope = sp.CreateScope();
                var runtime = scope.ServiceProvider.GetRequiredService<IRuntimeRepository>();


                await runtime.StopRuntime();
            }
        }
        public async Task<int> CanceledProccess()
        {

            try
            {
                await StopCurrentProccess();
                using var scope = sp.CreateScope();
                var runtime = scope.ServiceProvider.GetRequiredService<IRuntimeRepository>();
                var progress = scope.ServiceProvider.GetRequiredService<IProgressRepository>();
                await runtime.CancelRuntime();
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


        public async Task<List<QueryResult>> GetStats()
        {

            using var scope = sp.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDBContext>();
           var res=await context.stations.AsNoTracking().GroupBy(s=>new { s.Standard, s.Operator.VisibleCode, s.Operator.Name }).Select(s=>
                new QueryResult(s.Key.VisibleCode,s.Key.Standard, s.Count(),0,0,$"группировка станций по оператору {s.Key.Name}, типу сети {s.Key.Standard.ToString()}",false)
            ).ToListAsync();
            return res;
        }




    }
}
