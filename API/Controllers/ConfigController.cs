using API.Services.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    public class ConfigController : ControllerBase
    {
        private readonly IConfigService service;

        public ConfigController(IConfigService service)
        {
            this.service = service;
        }

        [HttpGet]
        [Route("getloaddataconfig")]
    
        public async Task<IActionResult> GetLoadDataConfigAsync()
        {
            var data = await service.LoadConfigurationAsync();
            return Ok(data);
        }
        [HttpPost]
        [Route("updateconfigdocumentsettings/{filter}")]
        public async Task<IActionResult> UpdateConfigDocumentSettingAsync(string filter, [FromBody] DocumentSettings setting) 
        {
            await service.UpdateDocumntSetting(filter, setting);
            return Ok();
        }

    }
}
