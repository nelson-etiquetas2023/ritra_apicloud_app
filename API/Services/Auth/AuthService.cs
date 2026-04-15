using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Dtos;
using Shared.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace API.Services.Auth
{
    public class AuthService(ApplicationDbContext context, IConfiguration configuration) : IAuthService 
    {
        public ApplicationDbContext Context { get; set; } = context;
        public IConfiguration Configuration { get; set; } = configuration;

        public async Task<ServiceResponse<string>> Login(string email, string password)
        {
            var response = new ServiceResponse<string>();
            try
            {
                var user = await Context.Users.FirstOrDefaultAsync(x =>
                x.Email.ToLower().Equals(email.ToLower()));

                if (user == null)
                {
                    response.Success = false;
                    response.Message = "User not found.";
                    Console.Write(response.Message);
                }
                else if (!VerifyPasswordHash(password, user.PasswordHash!, user.PasswordSalt!))
                {
                    response.Success = false;
                    response.Message = "Wrong password.";
                    Console.Write(response.Message);
                }
                else
                {
                    response.Data = CreateToken(user);
                    Console.Write(response.Message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return response;
        }

        public async Task<ServiceResponse<int>> Register(User user, string password)
        {
            if (await UserExists(user.Email)) 
            {
                return new ServiceResponse<int> { Success = false, Message ="User alredy exists." };
            }

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            return new ServiceResponse<int>
            {
                Data = user.Id,
                Success = true,
                Message = "usuario creado exitosamente...",
            };
        }

        public async Task<bool> UserExists(string email)
        {
            if (await Context.Users.AnyAsync(user =>
            user.Email.ToLower().Equals(email.ToLower())))
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt) 
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));

        }

        private static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt) 
        {
            using var hmac = new HMACSHA512(passwordSalt);
            var computeHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computeHash.SequenceEqual(passwordHash);
        }

        private string CreateToken(User user) 
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email)
            ];

            var mysecret = Configuration.GetSection("AppSettings:Token").Value!;

            if (string.IsNullOrEmpty(mysecret))
                throw new Exception("No se encontro la key principal en el appsettings...");

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8
                .GetBytes(mysecret));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.Now.AddDays(1),
                    signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;

        }


    }
}
