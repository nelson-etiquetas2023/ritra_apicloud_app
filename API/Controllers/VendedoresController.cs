using API.Services.Vendedores;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendedoresController(IVendedoresService service) : ControllerBase
    {
        private readonly IVendedoresService _service = service;

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAll()
        {
            var vendedores = await service.GetAllAsync();
            return Ok(vendedores);
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var vendedor = await service.GetByIdAsync(id);
            if (vendedor == null) return NotFound($"Vendedor {id} no encontrado");
            return Ok(vendedor);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Vendedor vendedor)
        {
            if (vendedor == null) return BadRequest("Datos inválidos");
            var created = await service.CreateAsync(vendedor);
            if (created == null)
                return Conflict("El vendedor no se pudo crear: el nombre está vacío o el código ya existe.");
            return CreatedAtAction(nameof(GetById), new { id = created.vendedor_id }, created);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Vendedor vendedor)
        {
            if (vendedor == null) return BadRequest("Datos inválidos");
            var updated = await service.UpdateAsync(id, vendedor);
            if (updated == null) return NotFound($"Vendedor {id} no encontrado");
            return Ok(updated);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await service.DeleteAsync(id);
            if (!deleted) return NotFound($"Vendedor {id} no encontrado");
            return NoContent();
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNum()
        {
            var next = await service.GetNextNumAsync();
            return Ok(new { numero = next });
        }
    }
}
