using Shared.Dtos;

namespace API.Services.PrintJobs
{
    public interface IPrintJobsService
    {
        Task<List<PrintJob>> GetPendingAsync();
        Task<PrintJob?> GetByIdAsync(int id);
        Task<(PrintJob? Job, string? Error)> EnqueueAsync(PrintJobEnqueueRequest request);
        Task<bool> ReorderAsync(List<PrintJobReorderItem> items);
        Task<bool> CancelAsync(int id);
        Task<(PrintJob? Job, string? Error)> RetryAsync(int id);
        Task<(string Zpl, string? Error)> BuildPreviewZplAsync(int? templateId, string? zpl, int? productId, int copies = 1);
    }
}
