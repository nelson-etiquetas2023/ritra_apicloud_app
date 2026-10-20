using API.Services.Config;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;
using Shared.Security;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ConfigController : ControllerBase
    {
        private readonly IConfigService service;

        public ConfigController(IConfigService service)
        {
            this.service = service;
        }

        [HttpGet]
        [Route("getloaddataconfig")]
        [Authorize(Policy = AppPolicies.AdminOnly)]

        public async Task<IActionResult> GetLoadDataConfigAsync()
        {
            var data = await service.LoadConfigurationAsync();
            return Ok(data);
        }
        [HttpPost]
        [Route("updateconfigdocumentsettings/{filter}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateConfigDocumentSettingAsync(string filter, [FromBody] DocumentSettings setting) 
        {
            await service.UpdateDocumntSetting(filter, setting);
            return Ok();
        }

        [HttpGet("categories")]
        [Authorize(Policy = AppPolicies.UserOrAdmin)]
        public async Task<IActionResult> GetCategoriesAsync()
        {
            return Ok(await service.GetCategoriesAsync());
        }

        [HttpPost("categories")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> CreateCategoryAsync([FromBody] Category category)
        {
            var created = await service.CreateCategoryAsync(category);
            return created == null
                ? Conflict("Ya existe una categoría con ese nombre o el nombre está vacío.")
                : Ok(created);
        }

        [HttpPut("categories/{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateCategoryAsync(int id, [FromBody] Category category)
        {
            var updated = await service.UpdateCategoryAsync(id, category);
            return updated == null
                ? Conflict("La categoría no existe, el nombre está vacío o ya está siendo utilizado.")
                : Ok(updated);
        }

        [HttpDelete("categories/{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteCategoryAsync(int id)
        {
            return await service.DeleteCategoryAsync(id) ? NoContent() : NotFound();
        }

        [HttpGet("units")]
        [Authorize(Policy = AppPolicies.UserOrAdmin)]
        public async Task<IActionResult> GetProductUnitsAsync() => Ok(await service.GetProductUnitsAsync());

        [HttpPost("units")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> CreateProductUnitAsync([FromBody] ProductUnit unit)
        {
            var created = await service.CreateProductUnitAsync(unit);
            return created == null ? Conflict("Ya existe una unidad con ese nombre o el nombre está vacío.") : Ok(created);
        }

        [HttpPut("units/{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateProductUnitAsync(int id, [FromBody] ProductUnit unit)
        {
            var updated = await service.UpdateProductUnitAsync(id, unit);
            return updated == null ? Conflict("La unidad no existe, el nombre está vacío o ya está siendo utilizado.") : Ok(updated);
        }

        [HttpDelete("units/{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteProductUnitAsync(int id)
        {
            return await service.DeleteProductUnitAsync(id) ? NoContent() : NotFound();
        }

        [HttpGet("warehouses")]
        [Authorize(Policy = AppPolicies.UserOrAdmin)]
        public async Task<IActionResult> GetWarehousesAsync() => Ok(await service.GetWarehousesAsync());

        [HttpGet("locations")]
        [Authorize(Policy = AppPolicies.UserOrAdmin)]
        public async Task<IActionResult> GetLocationsAsync() => Ok(await service.GetLocationsAsync());

        [HttpPost("locations")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> CreateLocationAsync([FromBody] Location location)
        {
            var created = await service.CreateLocationAsync(location);
            return created == null ? Conflict("El código ya existe en ese almacén o el código está vacío.") : Ok(created);
        }

        [HttpPut("locations/{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> UpdateLocationAsync(int id, [FromBody] Location location)
        {
            var updated = await service.UpdateLocationAsync(id, location);
            return updated == null ? Conflict("La ubicación no existe, el código está vacío o ya está siendo utilizado.") : Ok(updated);
        }

        [HttpDelete("locations/{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> DeleteLocationAsync(int id)
        {
            return await service.DeleteLocationAsync(id) ? NoContent() : NotFound();
        }

    }
}
