using HtmlAgilityPack;

namespace ConsoleAppProxys
{

    public record ProxyElement(string url,string type);
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Task.Run(GetProxys);
            Console.ReadLine();
        }


        public static async Task GetProxys()
        {
            using HttpClient client = new HttpClient();
            // чтобы имитировать браузер [citation:1]
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            var res = await client.GetAsync("https://proxymania.su/free-proxy?type=&country=RU&speed=");
            if (res.IsSuccessStatusCode)
            {
                var html=await res.Content.ReadAsStringAsync();
                Console.WriteLine(html);
                var list = ParseProxyTable(html);
                foreach (var item in list)
                {

                    Console.WriteLine($"\nrecord: url {item.url}, type {item.type}, full url {item.type.ToLower()}://{item.url}");
                }
            }

        }
        public static List<ProxyElement> ParseProxyTable(string html)
        {
            var proxies = new List<ProxyElement>();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tbody = doc.GetElementbyId("resultTable");

            if (tbody == null)
            {
                Console.WriteLine("Таблица с прокси не найдена!");
                return proxies;
            }

            var rows = tbody.SelectNodes(".//tr");

            if (rows == null || rows.Count == 0)
            {
                Console.WriteLine("Строки в таблице не найдены!");
                return proxies;
            }

            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");

                if (cells != null && cells.Count >= 3)
                {
                    var proxyCell = cells[0];

                    var typeCell = cells[2];

                    if (proxyCell != null && typeCell != null)
                    {
                        string url = proxyCell.InnerText.Trim();
                        string type = typeCell.InnerText.Trim();

                        if (!string.IsNullOrEmpty(type) &&
                            (type.Contains("HTTP") || type.Contains("SOCKS")))
                        {
                            proxies.Add(new ProxyElement(url, type));
                        }
                    }
                }
            }

            return proxies;
        }

    }
}
