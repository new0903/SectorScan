namespace WebAppCellMapper.DTO
{

    public class ProxyHandler
    {
        public string LastRequestId { get; set; }
        public DateTime LastUpdateRequestId { get; set; }
        public int CountTry { get; set; }
        public bool IsBan { get; set; }=false;
        public string UserAgent { get; set; }
        public HttpClientHandler ClientHandler { get; set; }

        public ProxyHandler()
        {

        }
        public ProxyHandler(HttpClientHandler clientHandler, string userAgent)
        {
            ClientHandler = clientHandler;
            UserAgent = userAgent;
        }
    }
}
