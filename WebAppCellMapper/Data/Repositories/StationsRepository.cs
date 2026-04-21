
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Data.Repositories
{
    public class StationsRepository : IStationsRepository
    {
        private readonly AppDBContext context;
        private readonly ILogger<StationsRepository> logger;

        public List<Station> StationsList {  get; set; }
        public int CountStations => StationsList.Count;


        public StationsRepository(AppDBContext context,ILogger<StationsRepository> logger)
        {
            StationsList = new List<Station>();
            this.context = context;
            this.logger = logger;
        }



        public async Task BulkSyncStationsAsync(CancellationToken ct)//Task<HttpResponseMessage> res,
        {
            try
            {

                if (StationsList != null && StationsList.Count > 0)
                {

                    await context.BulkInsertOrUpdateAsync(StationsList, cancellationToken: ct);
                    logger.LogInformation($"добавлено станций {StationsList.Count}");
                    StationsList.Clear();

                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Bulk sync was canceled");
            }
        }
        public async Task<List<QueryResult>> GetResult(CancellationToken ct)
        {
            var res = await context.stations.AsNoTracking().GroupBy(s => new { s.Standard, s.Operator.VisibleCode, s.Operator.Name }).Select(s =>
       new QueryResult(s.Key.VisibleCode, s.Key.Standard, s.Count(), 0, 0, $"группировка станций по оператору {s.Key.Name}, типу сети {s.Key.Standard.ToString()}", false)
   ).ToListAsync(ct);
            return res;
        }

    }
}
