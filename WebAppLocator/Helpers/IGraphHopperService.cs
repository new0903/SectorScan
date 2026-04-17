using WebAppLocator.DTO;

namespace WebAppLocator.Helpers
{
    public interface IGraphHopperService
    {
        Task<List<LocationPoint>> MatchRoadAsync(List<LocationPoint> points, CancellationToken cancellationToken=default);
    }
}
