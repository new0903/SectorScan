using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebAppCellMapper.DTO.Locator;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocatorController : ControllerBase
    {
        private readonly ILocatorService locatorService;

        public LocatorController(ILocatorService locatorService)
        {
            this.locatorService = locatorService;
        }


        [Authorize]
        [HttpPost("locate")]
        public async Task<IActionResult> GetLocate([FromBody]LocationRequest request)
        {
            var res=await locatorService.FindLocation(request,User.Identity.Name);
            if (res == null) BadRequest("Not found");
            return Ok(res);
        }


    }
}
