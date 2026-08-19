using API.Services.Enterprises;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnterprisesController(IEnterprisesService service, ILogger<EnterprisesController> logger) : ControllerBase
    {
        private readonly IEnterprisesService service = service;
        private readonly ILogger<EnterprisesController> _logger = logger;

        [HttpGet]
        [Route("getenterprise")]
        public async Task<IActionResult> GetEnterpriseAsync()
        {
            var enterprise = await service.GetEnterpriseAsync();
            return Ok(enterprise);
        }

        [HttpPost]
        [Route("createenterprise")]
        public async Task<IActionResult> CreateEnterpriseAsync(Enterprise enterprise)
        {
            var created = await service.CreateEnterpriseAsync(enterprise);
            return CreatedAtAction(nameof(GetEnterpriseAsync), new { }, created);
        }

        [HttpPut]
        [Route("updateenterprise")]
        public async Task<IActionResult> UpdateEnterpriseAsync([FromBody] ParametrosUpdateEnterprise parametros)
        {
            if (parametros?.enterprise == null)
                return BadRequest("Invalid enterprise data");

            var updated = await service.UpdateEnterpriseAsync(parametros.id, parametros.enterprise);
            if (updated == null)
                return NotFound();
            return Ok(updated);
        }
    }
}