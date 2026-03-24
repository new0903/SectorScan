namespace WebAppCellMapper.Options
{
    public class DatabaseConnection
    {
        public string HOST { get; set; }
        public string PORT { get; set; }
        public string DATABASE { get; set; }
        public string USER { get; set; }
        public string PASSWORD { get; set; }

        public override string? ToString()
            => $"Host={HOST};" +
                $"Port={PORT};" +
                $"Database={DATABASE};" +
                $"Username={USER};" +
                $"Password={PASSWORD}";
    }
}
