
using Microsoft.AspNetCore.Mvc;
using WebAppCellMapper.Services;
using Newtonsoft.Json;

namespace WebAppCellMapper.Controllers
{



    [Route("api/[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly IStationsScanningManager scanningManager;
        private readonly ILogger<StationsController> logger;

        public StationsController(IStationsScanningManager scanningManager, ILogger<StationsController> logger) 
        {
            this.scanningManager = scanningManager;
            this.logger = logger;
        }


        [HttpGet("fullscan")]     
        public async Task<IActionResult> FullScan()
        {
            try
            {
                scanningManager.StartFullScan();
                return Ok("started");
            }
            catch (Exception ex)
            {

               return BadRequest(ex.Message);
            }

        }


        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var res= scanningManager.GetCurrentProcess;
                return Ok(JsonConvert.SerializeObject(res));
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


        [HttpGet("stop")]
        public async Task<IActionResult> StopProccess()
        {
            try
            {
                await scanningManager.StopCurrentProccess();
                return Ok("stopped");
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }




        [HttpGet("canceled")]
        public async Task<IActionResult> CanceledProccess()
        {
            try
            {
                var c=await scanningManager.CanceledProccess();
                return Ok(c);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }

        [HttpGet("count")]
        public async Task<IActionResult> AllCountStations()
        {
            try
            {
                var res = await scanningManager.GetStats();
                return Ok(res);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }


    }
}
