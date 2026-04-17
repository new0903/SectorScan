using Domain.Enums;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Helpers;

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


    //public record OperatorDTO(long Id, string Code);

  
    public class ProgressDTO
    {
        public long Id { get; init; }
        public long OperatorId { get; init; }
        public string Code { get; init; }
        public NetworkStandard Standard { get; init; }
       // public DateTime UpdateAt { get; init; } = DateTime.UtcNow;


        public ProgressStatus Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        //public DateTime? LastProcessedAt { get; set; }
        public List<SquareSearch> Coordinates { get; set; }
        public int ScannedCount { get; set; }
        public int TotalCount { get; set; }
        public int AddedStationsCount { get; set; }


        public ProgressDTO()
        {

        }
        // Конструктор с автоматической установкой UpdateAt
        public ProgressDTO(
            long id,
            long operatorId,
            string code,
            NetworkStandard standard,
            ProgressStatus status,
            int scannedCount,
            int totalCount,
            int addedStationsCount,
            List<SquareSearch>? coordinates,
            DateTime? startedAt,
            DateTime? completedAt
            //DateTime? lastProcessedAt
            )
        {
            Id = id;
            OperatorId= operatorId;
            Code = code;
            Standard = standard;
            Status = status;
            StartedAt = startedAt;
            //LastProcessedAt = lastProcessedAt;
            CompletedAt = completedAt;

            Coordinates = coordinates ?? new List<SquareSearch>();
            ScannedCount = scannedCount;
            TotalCount = totalCount;
            AddedStationsCount = addedStationsCount;
        }

    }


}
