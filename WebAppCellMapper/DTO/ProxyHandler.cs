namespace WebAppCellMapper.DTO
{

    public class ProxyHandler
    {
        public string LastRequestId { get; set; }
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
