namespace WebAppCellMapper.Data.Models
{
    public class AppRuntimeState
    {
        public int Id { get; set; }
        public bool IsProcessing { get; set; }
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? FinishedAt { get; set; }
    }
}
