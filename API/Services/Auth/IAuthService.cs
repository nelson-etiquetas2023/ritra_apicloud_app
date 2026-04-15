using Shared.Security;
using Shared.Dtos;

namespace API.Services.Auth
{
    public interface IAuthService
    {
        Task<ServiceResponse<int>> Register(User user, string password);
        Task<bool> UserExists(string email);
        Task<ServiceResponse<string>> Login(string email, string password);
    }
}
