using WebAppLocator.Data.Models;
using WebAppLocator.DTO;
using WebAppLocator.DTO.Cells;

namespace WebAppLocator.Data.Repository
{
    public interface ILocationRepository
    {
        Task SaveLocation(string deviceId, LocationResponse response, DateTime? difTime = null);
        Task<LocationCell?> GetLastLocation(string deviceId, DateTime? difTime = null, CancellationToken ct=default);
        Task<List<LocationPoint>> GetListLastLocation(string deviceId, CancellationToken ct = default);

        Task<List<LocationCell>> GetStationsLocation(long[] ids, CancellationToken ct = default);
        Task<int> EraseOldLocation(CancellationToken ct=default);
    }
}
