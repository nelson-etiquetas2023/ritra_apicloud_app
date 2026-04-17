using Shared.Dtos;

namespace API.Services.Upload
{
    public interface IUploadService
    {
        Task<List<UploadResult>> UploadFilesAsync(List<IFormFile> files);
        Task<List<UploadResult>> GetAllImagesAsync();
        Task<UploadResult?> GetImageByIdAsync(int id);
        Task<bool> DeleteImageAsync(int id);
        Task<UploadResult?> SaveBase64ImageAsync(string base64Data, string originalFileName, string contentType);
    }
}
