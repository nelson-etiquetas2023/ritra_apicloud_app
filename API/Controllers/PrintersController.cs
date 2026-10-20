using API.Services.Printers;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;
using System.Net.Sockets;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintersController(IPrintersService service) : ControllerBase
    {
        [HttpGet]
        [Route("get")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await service.GetAllAsync());
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var printer = await service.GetByIdAsync(id);
            if (printer == null) return NotFound($"Impresora {id} no encontrada");
            return Ok(printer);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Printer printer)
        {
            if (printer == null) return BadRequest("Datos inválidos");
            var created = await service.CreateAsync(printer);
            if (created == null)
                return Conflict("La impresora no se pudo crear: el nombre está vacío o el código ya existe.");
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Printer printer)
        {
            if (printer == null) return BadRequest("Datos inválidos");
            var updated = await service.UpdateAsync(id, printer);
            if (updated == null) return NotFound($"Impresora {id} no encontrada");
            return Ok(updated);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await service.DeleteAsync(id);
            if (!deleted) return NotFound($"Impresora {id} no encontrada o tiene trabajos en cola");
            return NoContent();
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNum()
        {
            return Ok(new { numero = await service.GetNextNumAsync() });
        }

        [HttpPost]
        [Route("test/{id}")]
        public async Task<IActionResult> Test(int id)
        {
            var printer = await service.GetByIdAsync(id);
            if (printer == null) return NotFound($"Impresora {id} no encontrada");

            try
            {
                using var client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await client.ConnectAsync(printer.Ip, printer.Port, cts.Token);
                using var ns = client.GetStream();
                var zpl = "^XA\n^FO20,20^A0N,30,30^FDSCANPRO TEST^FS\n^PQ1,0,1,Y\n^XZ";
                var bytes = System.Text.Encoding.ASCII.GetBytes(zpl);
                await ns.WriteAsync(bytes, 0, bytes.Length, cts.Token);
                await ns.FlushAsync(cts.Token);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                return Ok(new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        [Route("calibrate/{id}")]
        public async Task<IActionResult> Calibrate(int id)
        {
            var (ok, error) = await service.CalibrateAsync(id);
            return Ok(new { ok, error });
        }
    }
}
