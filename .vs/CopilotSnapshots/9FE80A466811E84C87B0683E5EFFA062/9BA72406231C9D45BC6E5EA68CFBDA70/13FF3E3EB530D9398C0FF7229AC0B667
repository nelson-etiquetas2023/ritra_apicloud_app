using API.Services.Products;
using Microsoft.AspNetCore.Mvc;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IProductsService service) : ControllerBase
    {
        private readonly IProductsService service = service;

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
            if (producto == null)
            {
                Console.WriteLine($"GetProductById: Product {id} not found");
                return NotFound();
            }

            Console.WriteLine($"GetProductById: Product {id} found with {producto.Images?.Count ?? 0} images");
            foreach (var img in producto.Images ?? new List<ProductImage>())
            {
                Console.WriteLine($"  - Image ID: {img.Id}, ProductId: {img.ProductId}, ImageIndex: {img.ImageIndex}, FileName: {img.FileName}");
            }

            return Ok(producto);
        }

        [HttpGet]
        [Route("getproductbyid-debug/{id}")]
        public async Task<IActionResult> GetProductByIdDebug(int id) 
        {
            var producto = await service.GetProductByIdAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            // Crear una respuesta de debug
            var debugResponse = new
            {
                ProductId = producto.Product_id,
                Name = producto.Product_Name,
                ImagesCount = producto.Images?.Count ?? 0,
                Images = producto.Images?.Select(i => new
                {
                    i.Id,
                    i.ProductId,
                    i.ImageIndex,
                    i.FileName,
                    i.StoredFileName,
                    i.ContentType
                }).ToList()
            };

            return Ok(debugResponse);
        }

        [HttpPost]
        [Route("createproducts")]
        public async Task<IActionResult> CreateProductsAsync(Product producto) 
        {
            var created = await service.CreateproductAsync(producto);
            return CreatedAtAction(nameof(GetProductById), new { id = created.Product_id }, created);
        }

        [HttpPut]
        [Route("updateproducts")]
        public async Task<IActionResult> UpdateProductsAsync([FromBody] ParametrosUpdateProducts parametros)
        {
            var updated = await service.UpdateProductAsync(parametros.id, parametros.producto);
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

        [HttpPost]
        [Route("addproductimage/{productId}")]
        public async Task<IActionResult> AddProductImage(int productId, IFormFile file, [FromQuery] int imageIndex = 0)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            var success = await service.AddProductImageAsync(productId, file, imageIndex);
            if (!success)
                return NotFound("Product not found");

            return Ok(new { message = "Image uploaded successfully" });
        }

        [HttpDelete]
        [Route("deleteproductimage/{imageId}")]
        public async Task<IActionResult> DeleteProductImage(int imageId)
        {
            var success = await service.DeleteProductImageAsync(imageId);
            if (!success)
                return NotFound("Image not found");

            return Ok(new { message = "Image deleted successfully" });
        }

        [HttpGet]
        [Route("getproductimage/{imageId}")]
        public async Task<IActionResult> GetProductImage(int imageId)
        {
            var context = HttpContext.RequestServices.GetService(typeof(API.Data.ApplicationDbContext)) as API.Data.ApplicationDbContext;
            if (context == null)
                return StatusCode(500, "Database context not available");

            var image = await context.Images.FindAsync(imageId);
            if (image == null)
                return NotFound();

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", image.StoredFileName ?? string.Empty);
            if (!System.IO.File.Exists(uploadsPath))
                return NotFound("File not found on server");

            var bytes = await System.IO.File.ReadAllBytesAsync(uploadsPath);
            return File(bytes, image.ContentType ?? "application/octet-stream", image.FileName);
        }

        [HttpPost]
        [Route("createproductwithimages")]
        public async Task<IActionResult> CreateProductWithImagesAsync([FromBody] CreateProductWithImagesRequest request)
        {
            if (request == null || request.Product == null)
                return BadRequest("Invalid request");

            var created = await service.CreateProductWithImagesAsync(request);
            if (created == null)
                return StatusCode(500, "Error creating product with images");

            return CreatedAtAction(nameof(GetProductById), new { id = created.Product_id }, created);
        }

        [HttpPut]
        [Route("updateproductimage/{productId}")]
        public async Task<IActionResult> UpdateProductImageAsync(int productId, [FromBody] Base64ImageData imageData)
        {
            if (imageData == null)
                return BadRequest("Invalid image data");

            var success = await service.UpdateProductImageAsync(productId, imageData);
            if (!success)
                return StatusCode(500, "Error updating product image");

            return Ok(new { message = "Image updated successfully" });
        }
    }
}
