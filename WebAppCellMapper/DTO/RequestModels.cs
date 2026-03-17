using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.DTO
{
    public record QueryParams(double? latS, double? latE, double? lonS, double? lonE, double step = GeoBoundsService.EFFECTIVE_STEP);
 

    public record QueryResult(
        string OperatorCode,
        NetworkStandard Network,
        int CountAdded,
        int CountSectorsScaned,
        int CountSectors,
        string Message,
        bool isDone=false) 
    {
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }

}
