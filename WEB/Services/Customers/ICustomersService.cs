using Microsoft.AspNetCore.Components.Forms;
using Shared.Dtos;

namespace WEB.Services.Customers
{
    public interface ICustomersService
    {
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<bool> CreateCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(int id, Customer customer);
        Task<bool> DeleteCustomerAsync(int id);
        Task<CustomerImportResult> ImportCustomersFromExcelAsync(IBrowserFile file);
    }
}