using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.DTO;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Helpers
{
    public class RequestIdGenerator : IRequestIdGenerator
    {
        



        private readonly ILogger<RequestIdGenerator> logger;




        public RequestIdGenerator(ILogger<RequestIdGenerator> logger)
        {
            this.logger = logger;
        }

        public string GenerateRequestId(string path, string sessionId = "", string? userAgent = null)
        {
          //  if (IsValidHash(sessionId)) { logger.LogInformation("hash valid 1"); }
            if (string.IsNullOrEmpty(userAgent))
            {
                userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36";
            }

            string data = $"{path}-{sessionId ?? ""}-{userAgent}";
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            using var sha256 = SHA256.Create();
            byte[] hashBuffer = sha256.ComputeHash(bytes);

            return BitConverter.ToString(hashBuffer)
                .Replace("-", "")
                .ToLowerInvariant();
        }
       
        public async Task<bool> InitRequest(ProxyHandler handler, CancellationToken ct=default)
        {
            if (!string.IsNullOrEmpty(handler.LastRequestId) && handler.LastUpdateRequestId + TimeSpan.FromMinutes(25) > DateTime.UtcNow)
            {
                return true;
            }
            if (handler.CountTry>2)
            {
                handler.IsBan = true;
                return false;
            }
            handler.CountTry++;
            try
            {
                var id = GenerateRequestId("/api/Handbooks/countries?loadOperators=true",userAgent:handler.UserAgent);
                logger.LogInformation(id);
                using HttpClient client = new HttpClient(handler.ClientHandler, disposeHandler: false);
                //  HttpClient client = httpClient; // попробую по правилам посмотрим что выйдет
                client.BaseAddress = new Uri("https://4cells.ru:4444");


                client.DefaultRequestHeaders.UserAgent.ParseAdd(handler.UserAgent);
                // Добавляем заголовки для эмуляции AJAX/CORS запроса
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");//
                client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
                client.DefaultRequestHeaders.Add("Priority", "u=1, i");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
                client.DefaultRequestHeaders.Add("x-request-id", id);
                client.DefaultRequestHeaders.Add("Referer", "https://4cells.ru/");


                var res = await client.GetAsync($"/api/Handbooks/countries?loadOperators=true", ct);
                if (res.IsSuccessStatusCode) {

                    var content =await res.Content.ReadAsStringAsync();
                    var countryList = JsonConvert.DeserializeObject<List<Country>>(content);
                    //429 когда пустой массив
                    if (countryList!=null&&countryList.Count>0)
                    {
                        if (res.Headers.TryGetValues("x-request-id", out var requestIdValues))
                        {
                            handler.LastRequestId = requestIdValues.FirstOrDefault() ?? string.Empty;
                            handler.LastUpdateRequestId = DateTime.UtcNow;
                            //  if (IsValidHash(handler.LastRequestId)) { logger.LogInformation("hash valid 2"); }
                            if (string.IsNullOrEmpty(handler.LastRequestId))
                            {

                                return false;
                            }
                            return true;
                        }

                    }
                    return false;
                }

            }
            catch (OperationCanceledException)
            {
                logger.LogError("OperationCanceledException InitRequest");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            return false;

        }
    }
}
