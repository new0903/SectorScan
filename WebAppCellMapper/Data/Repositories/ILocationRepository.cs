

using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.DTO.Locator.Cells;

namespace WebAppCellMapper.Data.Repositories
{
    public interface ILocationRepository
    {
        Task SaveLocation(string deviceId, LocationResponse response, DateTime difTime );
        Task<LocationCell?> GetLastLocation(string deviceId, DateTime difTime, CancellationToken ct=default);
        Task<List<LocationPoint>> GetListLastLocation(string deviceId, CancellationToken ct = default);

        Task<List<LocationCell>> GetStationsLocation(long[] ids, CancellationToken ct = default);
        Task<int> EraseOldLocation(CancellationToken ct=default);
    }
}
