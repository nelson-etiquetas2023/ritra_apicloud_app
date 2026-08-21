using Shared.Dtos;

namespace API.Services.Customers
{
    public interface ICustomersService
    {
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer?> GetCustomerByIdAsync(int customerId);
        Task<Customer?> CreateCustomerAsync(Customer customer);
        Task<Customer?> UpdateCustomerAsync(int customerId, Customer customer);
        Task<bool> DeleteCustomerAsync(int customerId);
        Task<CustomerImportResult> ImportFromExcelAsync(Stream excelStream);
        Task<string> GetNextNumAsync();
    }
}