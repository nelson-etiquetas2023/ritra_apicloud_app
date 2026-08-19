using Shared.Dtos;

namespace API.Services.Suppliers
{
    public interface ISuppliersService
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int supplierId);
        Task<Supplier?> CreateSupplierAsync(Supplier supplier);
        Task<Supplier?> UpdateSupplierAsync(int id, Supplier supplier);
        Task<bool> DeleteSupplierAsync(int id);
        Task<SupplierImportResult> ImportFromExcelAsync(Stream excelStream);
    }
}
