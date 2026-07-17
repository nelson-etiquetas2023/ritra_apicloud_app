using API.Services.Inventory;
using API.Services.Reports;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderFisicoController(IInventoryService service, IReportsService reports) : ControllerBase
    {
        public IInventoryService Service { get; set; } = service;
        public IReportsService Reports { get; set; } = reports;

        [HttpPost]
        [Route("savenumberconsecinventory")]
        public async Task<bool> SaveNumeberConsecInventoryAsync([FromBody] NumeroFiltro Parametros) 
        {
            var number = Parametros.Numero;
            var filter = Parametros.Filtro;
            if (number == null || filter == null) return false; 
            await Service.SaveNumberConsecInventoryAsync(number!, filter);
            return true;
        }


        [HttpPost]
        [Route("updatedatascanproducts")]
        public async Task<bool> UpdateScanProductsAsync([FromBody] ScanProducts scanproduct) 
        {
            var updated = await Service.UpdateScanProductsAsync(scanproduct);
            if(!updated) return false;
            return true;
        }

        [HttpGet]
        [Route("getconfigbyid/{filter}")]
        public async Task<DocumentSettings> GetConfigById(string filter) 
        {
            return await Service.GetConfigById(filter);
        }

        [HttpGet]
        [Route("generatereportscanproducts/{id}")]
        public async Task GenerateReportsScanProductsAsync(string id) 
        {
            await Reports.GetReportScaProducts(id);
        }

        [HttpDelete]
        [Route("deletescanproducts/{id}")]
        public async Task<bool> DeleteScanProductsAsync(Guid id) 
        {
            var deleted = await Service.DeleteScanProductsAsync(id);
            if(!deleted) return false;
            return true;
        }


        [HttpGet]
        [Route("getscanproducts/{OrderId}")]
        public async Task<List<ScanProducts>> GetScanProductsAsync(string OrderId) 
        {
            var productsScan = await Service.GetScanProductsAsync(OrderId);
            return productsScan;
        }

        [HttpPost]
        [Route("savedatascanproducts")]
        public async Task<IActionResult> SaveDataProductScanAsync([FromBody] List<ScanProducts> productscan) 
        {
            var saved = await Service.SaveDataProductScanAsync(productscan);
            return Ok(saved);
        }


        [HttpGet]
        [Route("getorders")]

        public async Task<IActionResult> GetOrdersAsync()
        {
            var orders = await Service.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpGet]
        [Route("getorderbyid/{OrderNumber}")]
        public async Task<IActionResult> GetOrderByIdAsync(string OrderNumber) 
        {
            var order = await Service.GetOrderByIdAsync(OrderNumber);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpPost]
        [Route("createorder")]
        public async Task<IActionResult> CreateOrdersAsync([FromBody] OrderFisicoHeader order) 
        {
            var created = await Service.CreateOrderAsync(order);
            return Ok(created);
        }

        [HttpPut]
        [Route("updateorder/{orderNumber}")]
        public async Task<ActionResult> UpdateOrdersAsync(string orderNumber, [FromBody] OrderFisicoHeader order) 
        {
            var updated = await Service.UpdateOrderAsync(orderNumber, order);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deleteorder/{OrderNumber}")]
        public async Task<IActionResult> DeleteOrdersAsync(string OrderNumber) 
        {
            var deleted = await Service.DeleteOrderAsync(OrderNumber);
            if(!deleted) return NotFound();
            return NoContent();
        }
    }
}
