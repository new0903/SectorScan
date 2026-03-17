namespace WebAppCellMapper.Options
{
    public class DatabaseConnection
    {
        public string Host { get; set; }
        public string Port { get; set; }
        public string Database { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public override string? ToString()
            => $"Host={Host};" +
                $"Port={Port};" +
                $"Database={Database};" +
                $"Username={Username};" +
                $"Password={Password}";
    }
}
