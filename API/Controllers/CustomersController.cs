using API.Services.Customers;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController(ICustomersService service, ILogger<CustomersController> logger) : ControllerBase
    {
        private readonly ICustomersService service = service;
        private readonly ILogger<CustomersController> _logger = logger;

        [HttpGet]
        [Route("getcustomers")]
        public async Task<IActionResult> GetCustomersAsync()
        {
            var customers = await service.GetCustomersAsync();
            return Ok(customers);
        }

        [HttpGet]
        [Route("getcustomerbyid/{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var customer = await service.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound();
            return Ok(customer);
        }

        [HttpPost]
        [Route("createcustomers")]
        public async Task<IActionResult> CreateCustomersAsync([FromBody] Customer customer)
        {
            var created = await service.CreateCustomerAsync(customer);
            if (created == null)
                return Conflict("El cliente no se pudo crear: el nombre está vacío o ya existe un registro con ese código.");
            return CreatedAtAction(nameof(GetCustomerById), new { id = created.customer_id }, created);
        }

        [HttpPut]
        [Route("updatecustomers")]
        public async Task<IActionResult> UpdateCustomersAsync([FromBody] ParametrosUpdateCustomers parametros)
        {
            if (parametros?.customer == null)
                return BadRequest("Invalid customer data");

            var updated = await service.UpdateCustomerAsync(parametros.id, parametros.customer);
            if (updated == null)
                return NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deletecustomers/{id}")]
        public async Task<IActionResult> DeleteCustomersAsync(int id)
        {
            var deleted = await service.DeleteCustomerAsync(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }

        [HttpPost]
        [Route("import-excel")]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> ImportCustomersFromExcelAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No se recibió ningún archivo.");

            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest("El archivo debe ser un Excel (.xlsx o .xls).");

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                var result = await service.ImportFromExcelAsync(stream);
                _logger.LogInformation("Importación de clientes finalizada: insertados {Inserted}, actualizados {Updated}, omitidos {Skipped}",
                    result.Inserted, result.Updated, result.Skipped);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar clientes desde Excel");
                return StatusCode(500, new CustomerImportResult
                {
                    Errors = [new CustomerImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" }]
                });
            }
        }
    }
}