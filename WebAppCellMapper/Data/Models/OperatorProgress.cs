
using System.ComponentModel.DataAnnotations.Schema;
using WebAppCellMapper.Data.Enums;
using WebAppCellMapper.Helpers;

namespace WebAppCellMapper.Data.Models
{
    public enum ProgressStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed
    }
    public class OperatorProgress
    {

        /*только для чтения свойства*/
        public long Id { get; set; }
        public long OperatorId { get; set; }
        public Operator Operator { get; set; }
        public NetworkStandard Standard { get; set; }



        public int AddedStationsCount { get; set; }
        public int ScannedCount { get; set; }
        public int TotalCount { get; set; }
        [Column(TypeName = "jsonb")]
        public List<SquareSearch> Coordinates { get; set; }
        public ProgressStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}



