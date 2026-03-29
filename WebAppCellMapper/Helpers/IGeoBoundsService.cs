using System.Collections.Concurrent;

namespace WebAppCellMapper.Helpers
{
    public interface IGeoBoundsService
    {
        public List<SquareSearch> GetCoordianates(double latStart = GeoBoundsService.MIN_LAT, double latBorder = GeoBoundsService.MAX_LAT, double lonStart = GeoBoundsService.MIN_LON, double lonBorder = GeoBoundsService.MAX_LON, double step = GeoBoundsService.EFFECTIVE_STEP);
    }
}
