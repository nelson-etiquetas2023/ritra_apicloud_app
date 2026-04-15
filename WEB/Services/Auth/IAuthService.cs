using Shared.Dtos;
using Shared.Security;

namespace WEB.Services.Auth
{
    public interface IAuthService
    {
        Task<ServiceResponse<int>> Register(UserRegister user);
        Task<ServiceResponse<string>> Login(UserLogin user);

    }
}
