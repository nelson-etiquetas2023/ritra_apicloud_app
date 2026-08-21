using API.Services.Almacenes;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlmacenesController(IAlmacenesService service) : ControllerBase
    {
        private readonly IAlmacenesService _service = service;

        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAll()
        {
            var almacenes = await service.GetAllAsync();
            return Ok(almacenes);
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var almacen = await service.GetByIdAsync(id);
            if (almacen == null) return NotFound($"Almacén {id} no encontrado");
            return Ok(almacen);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Almacen almacen)
        {
            if (almacen == null) return BadRequest("Datos inválidos");
            var created = await service.CreateAsync(almacen);
            if (created == null)
                return Conflict("El almacén no se pudo crear: el nombre está vacío o el código ya existe.");
            return CreatedAtAction(nameof(GetById), new { id = created.almacen_id }, created);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Almacen almacen)
        {
            if (almacen == null) return BadRequest("Datos inválidos");
            var updated = await service.UpdateAsync(id, almacen);
            if (updated == null) return NotFound($"Almacén {id} no encontrado");
            return Ok(updated);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await service.DeleteAsync(id);
            if (!deleted) return NotFound($"Almacén {id} no encontrado");
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
