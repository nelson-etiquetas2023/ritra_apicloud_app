using API.Services.PrintJobs;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintJobsController(IPrintJobsService service) : ControllerBase
    {
        [HttpGet]
        [Route("pending")]
        public async Task<IActionResult> Pending()
        {
            return Ok(await service.GetPendingAsync());
        }

        [HttpGet]
        [Route("getbyid/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var job = await service.GetByIdAsync(id);
            if (job == null) return NotFound($"Trabajo {id} no encontrado");
            return Ok(job);
        }

        [HttpPost]
        [Route("enqueue")]
        public async Task<IActionResult> Enqueue([FromBody] PrintJobEnqueueRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos");
            var (job, error) = await service.EnqueueAsync(request);
            if (job == null) return BadRequest(error ?? "No se pudo encolar el trabajo.");
            return Ok(job);
        }

        [HttpPost]
        [Route("reorder")]
        public async Task<IActionResult> Reorder([FromBody] List<PrintJobReorderItem> items)
        {
            if (items == null || items.Count == 0) return BadRequest("Datos inválidos");
            var ok = await service.ReorderAsync(items);
            if (!ok) return BadRequest("No se pudo reordenar.");
            return Ok(new { ok = true });
        }

        [HttpPost]
        [Route("cancel/{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var ok = await service.CancelAsync(id);
            if (!ok) return NotFound($"Trabajo {id} no encontrado o ya finalizado");
            return Ok(new { ok = true });
        }

        [HttpPost]
        [Route("retry/{id}")]
        public async Task<IActionResult> Retry(int id)
        {
            var (job, error) = await service.RetryAsync(id);
            if (job == null) return BadRequest(error ?? $"Trabajo {id} no encontrado");
            return Ok(job);
        }
    }
}
