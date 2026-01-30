using API.Services.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsService service;
        public ProductsController(IProductsService service)
        {
            this.service = service;
        }

        [HttpGet]
        [Route("getproducts")]
        public async Task<IActionResult> GetProductsAsync()
        {
            var products = await service.GetProductAsync();
            return Ok(products);
        }

        [HttpGet]
        [Route("getproductbyid/{id}")]
        public async Task<IActionResult> GetProductById(int id) 
        {
            var producto = await service.GetProductByIdAsync(id);
            if (producto == null) return NotFound();
            return Ok(producto);
        }

        [HttpPost]
        [Route("createproducts")]
        public async Task<IActionResult> CreateProductsAsync(Product producto) 
        {
            var created = await service.CreateproductAsync(producto);
            return CreatedAtAction(nameof(GetProductById), new { id = created.Product_id }, created);
        }

        [HttpPut]
        [Route("updateproducts/{id}")]
        public async Task<IActionResult> UpdateProductsAsync(int id, Product producto)
        {
            var updated = await service.UpdateProductAsync(id, producto);
            if (updated == null) NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deleteproducts/{id}")]
        public async Task<IActionResult> DeleteProductsAsync(int id) 
        {
            var deleted = await service.DeleteProductAsync(id);
            if (!deleted) NotFound();
            return NoContent();
        }

    }
}
