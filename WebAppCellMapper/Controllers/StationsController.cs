using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Services;
using System.Text;
using WebAppCellMapper.Data.Models;
using WebAppCellMapper.Services;
using WebAppCellMapper.DTO;
using Newtonsoft.Json;

namespace WebAppCellMapper.Controllers
{



    [Route("api/[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly IStationsService stationsService;
        private readonly ILogger<StationsController> logger;

        public StationsController(IStationsService stationsService, ILogger<StationsController> logger) 
        {
            this.stationsService = stationsService;
            this.logger = logger;
        }


        /// <summary>
        /// Пишем ответ клиенту
        /// </summary>
        /// <remarks>
        /// Этот метод для эндпоинта SSE формата
        /// </remarks>
        private async Task WriteResponse(string json)
        {
            var message = $"data: {json}\n\n";
            var bytes = Encoding.UTF8.GetBytes(message);
            await Response.Body.WriteAsync(bytes, 0, bytes.Length);
            await Response.Body.FlushAsync();
        }

        [HttpGet]
        [Produces("text/event-stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task SyncStationsAll( CancellationToken ct = default)
        {
            logger.LogInformation($"SearchByOperator start");
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cashe-Control", "no-cashe");
            Response.Headers.Add("Connections", "keep-alive");
            try
            {


                await foreach (var item in stationsService.SyncStationsAllAsync(ct))
                {
                    await WriteResponse(JsonConvert.SerializeObject(item));
                    if (item.isDone)
                    {
                        await WriteResponse("[DONE]");
                        return;  // Выходим из цикла
                    }
                }
            }
            catch (Exception ex)
            {

                logger.LogError(ex.Message);
                throw;
            }

        }
        [HttpGet("{network}/{operatorCode}")]
        [Produces("text/event-stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task SearchByOperator(NetworkStandard network, string operatorCode, [FromQuery]QueryParams queryParams, CancellationToken ct = default)
        {
            logger.LogInformation($"SearchByOperator start");
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cashe-Control", "no-cashe");
            Response.Headers.Add("Connections", "keep-alive");
            try
            {
               
                logger.LogInformation($"SearchByOperator data valid");
                if (queryParams != null)
                {
                    if (!queryParams.latS.HasValue || !queryParams.latE.HasValue || !queryParams.lonS.HasValue || !queryParams.lonE.HasValue)
                    {
                        Response.StatusCode = 400;
                        return;
                    }
                    await foreach (var item in stationsService.ScanAreaAsync(operatorCode, network,
                        queryParams.latS.Value, queryParams.latE.Value,
                        queryParams.lonS.Value, queryParams.lonE.Value,
                        queryParams.step, ct))
                    {
                        await WriteResponse(JsonConvert.SerializeObject(item));
                        if (item.isDone)
                        {
                            await WriteResponse("[DONE]");
                            return;  // Выходим из цикла
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {

                logger.LogError(ex.Message);
                throw;
            }

        }

     


    }
}
