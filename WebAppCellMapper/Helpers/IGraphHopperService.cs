

using WebAppCellMapper.DTO.Locator;

namespace WebAppCellMapper.Helpers
{
    public interface IGraphHopperService
    {
        Task<List<LocationPoint>> MatchRoadAsync(List<LocationPoint> points, CancellationToken cancellationToken=default);
    }
}
