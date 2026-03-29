using System.Runtime.CompilerServices;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;

namespace WebAppCellMapper.Services
{
    public interface IStationsService
    {
        public IAsyncEnumerable<QueryResult> SyncStationsAllAsync( CancellationToken ct = default);
    }
}
