using Domain.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Data.Repositories
{
    public interface IStationsRepository
    {
        List<Station> StationsList { get; set; }
        int CountStations {  get; }
        Task BulkSyncStationsAsync(CancellationToken ct = default);
        Task<List<QueryResult>> GetResult(CancellationToken ct = default);
    }
}
