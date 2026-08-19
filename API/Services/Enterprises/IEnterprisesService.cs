using Shared.Dtos;

namespace API.Services.Enterprises
{
    public interface IEnterprisesService
    {
        Task<Enterprise?> GetEnterpriseAsync();
        Task<Enterprise> CreateEnterpriseAsync(Enterprise enterprise);
        Task<Enterprise?> UpdateEnterpriseAsync(int enterpriseId, Enterprise enterprise);
    }
}