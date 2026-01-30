using Shared.Dtos;

namespace API.Services.Users
{
    public interface IUsersService
    {
        Task<List<User>> GetUsersAsync();
        Task<User?> GetUsersByIdAsync(int UserId);
        Task<User> CreateUserAsync(User usuario);
        Task<User?> UpdateUserAsync(int UserId, User usuario);
        Task<bool> DeleteUserAsync(int UserId);
    }
}

