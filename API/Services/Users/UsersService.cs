using API.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Dtos;

namespace API.Services.Users
{
    public class UsersService : IUsersService
    {
        public readonly ApplicationDbContext context;

        public UsersService(ApplicationDbContext context)
        {
           this.context = context;
        }


        public async Task<List<User>> GetUsersAsync()
        {
            return await context.Users.ToListAsync();
        }

        public async Task<User?> GetUsersByIdAsync(int UserId)
        {
            return await context.Users.FindAsync(UserId);
        }

        public async Task<User> CreateUserAsync(User usuario)
        {
            context.Users.Add(usuario);
            await context.SaveChangesAsync();
            return usuario;
        }

        public async Task<User?> UpdateUserAsync(int UserId, User usuario)
        {
            var existing = await context.Users.FindAsync(UserId);
            if (existing == null) return null;
            existing.User_Name = usuario.User_Name;
            existing.Login  = usuario.Login;
            existing.Password = usuario.Password;

            await context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteUserAsync(int UserId)
        {
            var user = await context.Users.FindAsync(UserId);
            if (user == null) return false;
            context.Users.Remove(user);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
