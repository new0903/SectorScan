using System.Runtime.CompilerServices;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    public interface IStationsService
    {
        public IAsyncEnumerable<QueryResult> SyncStationsAllAsync( CancellationToken ct = default);
        public IAsyncEnumerable<QueryResult> ScanAreaAsync(string operatorCode, NetworkStandard network, double latS, double latE, double lonS, double lonE, double step , CancellationToken ct = default);
    }
}
