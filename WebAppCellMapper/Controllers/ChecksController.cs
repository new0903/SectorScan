using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebAppCellMapper.Data;

namespace WebAppCellMapper.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ChecksController : ControllerBase
    {
        private readonly AppDBContext context;

        public ChecksController(AppDBContext context)
        {
            this.context = context;
        }



        [Route("/ready")]
        [HttpGet]
        public async Task<IActionResult> Ready()
        {
           var res= await context.CheckHealthAsync(new HealthCheckContext());
            if (res.Status==HealthStatus.Healthy)
            {
                
                return Ok("2хх");
            }
            return StatusCode(503, new { title = res.Description });
        }




        [Route("/alive")]
        [HttpGet]
        public async Task<IActionResult> Alive()
        {
            return Ok("2хх");
        }



    }
}
