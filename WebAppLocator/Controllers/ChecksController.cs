
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebAppLocator.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ChecksController : ControllerBase
    {
        //private readonly AppDBContext context;
        //private readonly NpgsqlDataSource npgsqlData;
        private readonly HealthCheckService healthCheckService;

        public ChecksController(HealthCheckService healthCheckService)
        {

            this.healthCheckService = healthCheckService;
        }



        [Route("/ready")]
        [HttpGet]
        public async Task<IActionResult> Ready()
        {
            var report = await healthCheckService.CheckHealthAsync();

            if (report.Status == HealthStatus.Healthy)
            {
                return Ok(
                    new
                    {
                        status = "2хх",
                        timestamp = DateTime.UtcNow,
                        checks = report.Entries.ToDictionary(
                        x => x.Key,
                        x => new { status = x.Value.Status.ToString(), description = x.Value.Description }
                    )});
            }

            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                errors = report.Entries
                    .Where(x => x.Value.Status != HealthStatus.Healthy)
                    .Select(x => $"{x.Key}: {x.Value.Description ?? "Failed"}")
                    .ToList()
            });


      
        }




        [Route("/alive")]
        [HttpGet]
        public IActionResult Alive(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(503);
            }

            return Ok("2xx");
        }



    }
}
