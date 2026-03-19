
using Microsoft.AspNetCore.Mvc;
using WebAppCellMapper.Services;

namespace WebAppCellMapper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperatorsController : ControllerBase
    {
        private readonly IOperatorsService operatorsService;

        public OperatorsController(IOperatorsService operatorsService) 
        {
            this.operatorsService = operatorsService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {

            var res=await operatorsService.GetOperators();
            return Ok(res);
        }
    }
}
