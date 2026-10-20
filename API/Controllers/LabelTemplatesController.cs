using API.Services.LabelTemplates;
using API.Services.PrintJobs;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabelTemplatesController(
        ILabelTemplatesService service,
        IPrintJobsService jobsService,
        IHttpClientFactory httpFactory) : ControllerBase
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
            var template = await service.GetByIdAsync(id);
            if (template == null) return NotFound($"Plantilla {id} no encontrada");
            return Ok(template);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] LabelTemplate template)
        {
            if (template == null) return BadRequest("Datos inválidos");
            var created = await service.CreateAsync(template);
            if (created == null)
                return Conflict("La plantilla no se pudo crear: el nombre está vacío o el código ya existe.");
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] LabelTemplate template)
        {
            if (template == null) return BadRequest("Datos inválidos");
            var updated = await service.UpdateAsync(id, template);
            if (updated == null) return NotFound($"Plantilla {id} no encontrada");
            return Ok(updated);
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await service.DeleteAsync(id);
            if (!deleted) return NotFound($"Plantilla {id} no encontrada o tiene trabajos en cola");
            return NoContent();
        }

        [HttpGet]
        [Route("getnextnum")]
        public async Task<IActionResult> GetNextNum()
        {
            return Ok(new { numero = await service.GetNextNumAsync() });
        }

        [HttpGet]
        [Route("default")]
        public async Task<IActionResult> GetDefault()
        {
            return Ok(await service.GetDefaultAsync());
        }

        [HttpPost]
        [Route("setdefault/{id}")]
        public async Task<IActionResult> SetDefault(int id)
        {
            var (template, error) = await service.SetDefaultAsync(id);
            if (template == null) return BadRequest(error ?? $"Plantilla {id} no válida");
            return Ok(template);
        }

        [HttpPost]
        [Route("{id}/duplicate")]
        public async Task<IActionResult> Duplicate(int id)
        {
            var clone = await service.DuplicateAsync(id);
            if (clone == null) return NotFound($"Plantilla {id} no encontrada");
            return Ok(clone);
        }

        [HttpPost]
        [Route("preview-zpl")]
        public async Task<IActionResult> PreviewZpl([FromBody] PreviewZplRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos");
            var (zpl, error) = await jobsService.BuildPreviewZplAsync(request.TemplateId, request.Zpl, request.ProductId, request.Copies);
            if (error != null) return BadRequest(error);
            return Ok(new { zpl });
        }

        [HttpGet]
        [Route("preview-png/{id}")]
        public async Task<IActionResult> PreviewPng(int id, [FromQuery] int? productId, [FromQuery] int? dpmm)
        {
            var template = await service.GetByIdAsync(id);
            if (template == null) return NotFound($"Plantilla {id} no encontrada");

            var (zpl, error) = await jobsService.BuildPreviewZplAsync(id, null, productId);
            if (error != null) return BadRequest(error);

            int dots;
            if (dpmm is 6 or 8 or 12 or 24)
                dots = dpmm.Value;
            else
                dots = template.Dpi switch { 152 => 6, 300 => 12, 600 => 24, _ => 8 };
            var w = template.WidthIn.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            var h = template.HeightIn.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            var url = $"https://api.labelary.com/v1/printers/{dots}dpmm/labels/{w}x{h}/0/";

            try
            {
                var client = httpFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(20);
                var content = new StringContent(zpl, System.Text.Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = await client.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadAsStringAsync();
                    return BadRequest($"Labelary {(int)response.StatusCode}: {detail}");
                }
                var png = await response.Content.ReadAsByteArrayAsync();
                return File(png, "image/png");
            }
            catch (Exception ex)
            {
                return StatusCode(502, $"No se pudo renderizar en Labelary: {ex.Message}");
            }
        }
    }
}
