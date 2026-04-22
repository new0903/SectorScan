using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.Options;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocatorController : ControllerBase
    {
        private readonly ILocatorService locatorService;
        private readonly LocatorTestOptions _optionsSetup;

        public LocatorController(ILocatorService locatorService, IOptions<LocatorTestOptions> optionsSetup)
        {
            this.locatorService = locatorService;
            _optionsSetup = optionsSetup.Value;
        }


       // [Authorize]
        [HttpPost("locate")]
        public async Task<IActionResult> GetLocate([FromQuery]string apikey, [FromBody]LocationRequest request)
        {
            if (_optionsSetup.ApiKey.Contains(apikey))
            {
                var res = await locatorService.FindLocation(request, apikey);
                if (res == null) return BadRequest("Not found");
                return Ok(res);
            }
            return Unauthorized();
        }


    }
}
