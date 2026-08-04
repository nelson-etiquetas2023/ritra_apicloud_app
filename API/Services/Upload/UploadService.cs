using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;
using System.Net;

namespace API.Services.Upload
{
    public class UploadService(IWebHostEnvironment environment, ApplicationDbContext context, ILogger<UploadService> logger) : IUploadService
    {
        private readonly IWebHostEnvironment _environment = environment;
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<UploadService> _logger = logger;

        public async Task<List<UploadResult>> UploadFilesAsync(List<IFormFile> files)
        {
            var uploadResults = new List<UploadResult>();

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var uploadResult = new UploadResult
                    {
                        FileName = file.FileName,
                        StoredFileName = Path.GetRandomFileName(),
                        ContentType = file.ContentType
                    };

                    try
                    {
                        var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                        if (!Directory.Exists(uploadsPath))
                        {
                            Directory.CreateDirectory(uploadsPath);
                        }

                        var filePath = Path.Combine(uploadsPath, uploadResult.StoredFileName);

                        await using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        _logger.LogInformation("Archivo guardado exitosamente: {FileName} en {FilePath}", file.FileName, filePath);

                        _context.Uploads.Add(uploadResult);
                        uploadResults.Add(uploadResult);
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        _logger.LogError(ex, "Error de permisos al guardar archivo {FileName}. Verifica los permisos de la carpeta uploads.", file.FileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al guardar archivo {FileName}", file.FileName);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return uploadResults;
        }

        public async Task<List<UploadResult>> GetAllImagesAsync()
        {
            return await _context.Uploads
                .Select(u => new UploadResult
                {
                    Id = u.Id,
                    FileName = u.FileName,
                    StoredFileName = u.StoredFileName,
                    ContentType = u.ContentType
                })
                .ToListAsync();
        }

        public async Task<UploadResult?> GetImageByIdAsync(int id)
        {
            return await _context.Uploads.FindAsync(id);
        }

        public async Task<bool> DeleteImageAsync(int id)
        {
            try
            {
                var upload = await _context.Uploads.FindAsync(id);
                if (upload == null)
                    return false;

                // Eliminar archivo del disco
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", upload.StoredFileName!);
                if (File.Exists(uploadsPath))
                {
                    File.Delete(uploadsPath);
                }

                // Eliminar registro de la BD
                _context.Uploads.Remove(upload);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando imagen con ID {ImageId}", id);
                return false;
            }
        }

        public async Task<UploadResult?> SaveBase64ImageAsync(string base64Data, string originalFileName, string contentType)
        {
            try
            {
                // Decodificar base64
                var base64Index = base64Data.IndexOf(',');
                if (base64Index >= 0)
                {
                    base64Data = base64Data.Substring(base64Index + 1);
                }

                var imageBytes = Convert.FromBase64String(base64Data);

                // Generar nombre seguro con Guid
                var fileExtension = Path.GetExtension(originalFileName);
                var storedFileName = $"{Guid.NewGuid()}{fileExtension}";

                var uploadResult = new UploadResult
                {
                    FileName = originalFileName,
                    StoredFileName = storedFileName,
                    ContentType = contentType
                };

                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                var filePath = Path.Combine(uploadsPath, storedFileName);

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                _logger.LogInformation("Imagen base64 guardada: {FileName} en {FilePath}", originalFileName, filePath);

                _context.Uploads.Add(uploadResult);
                await _context.SaveChangesAsync();

                return uploadResult;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Error de permisos al guardar imagen base64 {FileName}. Verifica los permisos de la carpeta uploads.", originalFileName);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving base64 image: {ErrorMessage}", ex.Message);
                return null;
            }
        }
    }
}
