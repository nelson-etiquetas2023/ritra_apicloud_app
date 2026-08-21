using Microsoft.AspNetCore.Components.Forms;
using Shared.Dtos;

namespace WEB.Services.Suppliers
{
    public interface ISuppliersService
    {
        Task<List<Supplier>> GetSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<bool> CreateSupplierAsync(Supplier supplier);
        Task<bool> UpdateSupplierAsync(int id, Supplier supplier);
        Task<bool> DeleteSupplierAsync(int id);
        Task<string> GetNextNumAsync();
        Task<SupplierImportResult> ImportSuppliersFromExcelAsync(IBrowserFile file);
    }
}
