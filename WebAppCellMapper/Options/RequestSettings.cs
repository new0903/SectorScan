namespace WebAppCellMapper.Options
{
    public class RequestSettings
    {

        public int MaxConnectionsPerServer { get; set; }
        public int TimeoutSeconds { get; set; }
        public int TimeoutUpdateProxy { get; set; }
        public int RandomStartRequestSeconds { get; set; }
    }
}
