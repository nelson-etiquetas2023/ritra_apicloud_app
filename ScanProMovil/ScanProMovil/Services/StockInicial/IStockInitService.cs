using ScanProMovil.Entities;

namespace ScanProMovil.Services.StockInicial
{
    public interface IStockInitService
    {
        Task<List<StockInit>> GetAllAsync();
        Task<List<StockInit>> SearchAsync(string searchText);
        Task<StockInit?> GetByIdAsync(string numero);
        Task<bool> CreateAsync(StockInit doc);
        Task<bool> UpdateAsync(StockInit doc);
        Task<bool> DeleteAsync(string numero);
        Task<string> GetNextNumberAsync();
        Task<bool> SeedDummyDataAsync();
        Task<SincroStockInitResult> SincronizarAsync(string numero);
    }

    public class SincroStockInitResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}