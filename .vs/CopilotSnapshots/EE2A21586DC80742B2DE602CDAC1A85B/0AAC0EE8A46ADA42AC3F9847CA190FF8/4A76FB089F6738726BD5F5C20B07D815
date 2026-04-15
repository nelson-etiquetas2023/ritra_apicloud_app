using API.Data;
using API.Services.Upload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using System.Net;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class UploadController(IUploadService uploadService, ApplicationDbContext context) : ControllerBase
    {
        private readonly IUploadService _uploadService = uploadService;
        public ApplicationDbContext context = context;

        [HttpGet]
        [Route("getimages")]
        public async Task<ActionResult<List<UploadResult>>> GetAllImagesAsync() 
        {
            var images = await _uploadService.GetAllImagesAsync();
            return Ok(images);
        }

        [HttpGet]
        [Route("getimagenbyid")]
        public async Task<IActionResult> GetImagen(int id) 
        {
            var upload = await _uploadService.GetImageByIdAsync(id);
            if(upload == null) 
                return NotFound();

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", upload.StoredFileName!);
            if (!System.IO.File.Exists(uploadsPath))
                return NotFound("Archivo no encontrado en el servidor");

            var bytes = await System.IO.File.ReadAllBytesAsync(uploadsPath);
            return File(bytes, upload.ContentType ?? "application/octet-stream", upload.FileName);
        }

        [HttpPost]
        [Route("uploadfile")]
        public async Task<ActionResult<List<UploadResult>>> UploadFile([FromForm] List<IFormFile> files) 
        {
            if (files == null || files.Count == 0)
                return BadRequest("No files provided");

            var uploadResults = await _uploadService.UploadFilesAsync(files);
            return Ok(uploadResults);
        }

        [HttpDelete]
        [Route("deleteimage")]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var success = await _uploadService.DeleteImageAsync(id);
            if (!success)
                return NotFound("Imagen no encontrada");

            return Ok(new { message = "Imagen eliminada correctamente" });
        }
    }
}
