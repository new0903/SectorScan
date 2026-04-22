using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.Options;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Controllers
{
    [Route("v1")]
    [ApiController]
    public class LocatorController : ControllerBase
    {
        private readonly ILocatorService locatorService;

        string[] keys = {

  
          "c07999d9410506831250dc66450412cdd54c36c109344853971b24b2047acabe",
          "9136120ec67236dcd99746fd02505da492e4280818f5fb6b0e1466277c7eb160",
          "6d8c76d9d2efb53aa5cebb816cf8cd2115cc087a9a8f44762c6cd11ca97e0862",
          "72ba9beb79a97978eaae70140e90764715518ee4fcfc29d607c625896d445ac7",
          "b502ad5e0429429abc59c69592095e84100dd27c8d8b7cf48ce5d85b66395cb1"
        
        };

        public LocatorController(ILocatorService locatorService)
        {
            this.locatorService = locatorService;
        }


       // [Authorize]
        [HttpPost("locate")]
        public async Task<IActionResult> GetLocate([FromQuery]string apikey, [FromBody]LocationRequest request)
        {
            if (keys.Contains(apikey))
            {
                var res = await locatorService.FindLocation(request, apikey);
                if (res == null) return BadRequest("Not found");
                return Ok(res);
            }
            return Unauthorized();
        }


    }
}
