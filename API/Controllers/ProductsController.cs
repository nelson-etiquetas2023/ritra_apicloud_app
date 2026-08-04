using API.Services.Products;
using Microsoft.AspNetCore.Authorization;
using API.Storage;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Shared.Dtos;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
   
    public class ProductsController(IProductsService service, ILogger<ProductsController> logger, IWebHostEnvironment environment, IConfiguration configuration) : ControllerBase
    {
        private readonly IProductsService service = service;
        private readonly ILogger<ProductsController> _logger = logger;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly IConfiguration _configuration = configuration;

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

        [HttpPost]
        [Route("createproductwithfiles")]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> CreateProductWithFilesAsync()
        {
            _logger.LogInformation("CreateProductWithFilesAsync: ContentLength={ContentLength}", Request.ContentLength);

            var form = await Request.ReadFormAsync();
            var productJson = form["product"].FirstOrDefault();
            var files = form.Files;

            if (string.IsNullOrEmpty(productJson))
            {
                _logger.LogWarning("CreateProductWithFilesAsync: product form field missing");
                return BadRequest("Missing product data");
            }

            Product? product = null;
            try
            {
                product = JsonSerializer.Deserialize<Product>(productJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateProductWithFilesAsync: error deserializing product JSON");
                return BadRequest("Invalid product JSON");
            }

            if (product == null)
            {
                _logger.LogWarning("CreateProductWithFilesAsync: deserialized product is null");
                return BadRequest("Invalid product data");
            }

            var created = await service.CreateProductWithFilesAsync(product, files);
            if (created == null)
            {
                _logger.LogError("CreateProductWithFilesAsync: service returned null");
                return StatusCode(500, "Error creating product with files");
            }

            return CreatedAtAction(nameof(GetProductById), new { id = created.Product_id }, created);
        }

        [HttpPost]
        [Route("bulkcreateproducts")]
        public async Task<IActionResult> BulkCreateProductsAsync([FromBody] List<Product> products)
        {
            var count = await service.BulkCreateProductsAsync(products);
            return Ok(new { count });
        }

        [HttpPost]
        [Route("import-excel")]
        [RequestSizeLimit(30_000_000)]
        public async Task<IActionResult> ImportProductsFromExcelAsync(IFormFile file)
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
                _logger.LogInformation("Importación de productos finalizada: insertados {Inserted}, actualizados {Updated}, omitidos {Skipped}",
                    result.Inserted, result.Updated, result.Skipped);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar productos desde Excel");
                return StatusCode(500, new ProductImportResult
                {
                    Errors = [new ProductImportError { Row = 0, Message = $"Error interno al procesar el archivo: {ex.Message}" }]
                });
            }
        }

        [HttpPut]
        [Route("updateproducts")]
        public async Task<IActionResult> UpdateProductsAsync([FromBody] ParametrosUpdateProducts parametros)
        {
            if (parametros?.producto == null)
                return BadRequest("Invalid product data");

            var updated = await service.UpdateProductAsync(parametros.id, parametros.producto);
            if (updated == null) 
                return NotFound();
            return Ok(updated);
        }

        [HttpDelete]
        [Route("deleteproducts/{id}")]
        public async Task<IActionResult> DeleteProductsAsync(int id) 
        {
            var deleted = await service.DeleteProductAsync(id);
            if (!deleted) 
                return NotFound();
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

            var filePath = Path.Combine(ProductImageStorage.GetPath(_environment, _configuration), image.StoredFileName ?? string.Empty);
            if (!System.IO.File.Exists(filePath))
                return NotFound("File not found on server");

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            return File(bytes, image.ContentType ?? "application/octet-stream", image.FileName);
        }

        [HttpPost]
        [Route("createproductwithimages")]
        [RequestSizeLimit(long.MaxValue)]
        public async Task<IActionResult> CreateProductWithImagesAsync([FromBody] CreateProductWithImagesRequest request)
        {
            _logger.LogInformation("CreateProductWithImagesAsync: ContentLength={ContentLength}", Request.ContentLength);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            }

            if (request == null || request.Product == null)
            {
                _logger.LogWarning("CreateProductWithImagesAsync: request or request.Product is null");
                return BadRequest("Invalid request");
            }

            var created = await service.CreateProductWithImagesAsync(request);
            if (created == null)
            {
                _logger.LogError("CreateProductWithImagesAsync: service returned null when creating product {ProductName}", request.Product.Product_Name);
                return StatusCode(500, "Error creating product with images");
            }

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
