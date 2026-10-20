using Shared.Dtos;

namespace WEB.Services.Enterprises
{
    public interface IEnterprisesService
    {
        Task<Enterprise?> GetEnterpriseAsync();
        Task<List<Enterprise>> GetAllAsync();
        Task<bool> CreateEnterpriseAsync(Enterprise enterprise);
        Task<bool> UpdateEnterpriseAsync(int id, Enterprise enterprise);
    }
}