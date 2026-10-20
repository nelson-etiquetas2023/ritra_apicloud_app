using API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Dtos;
using Shared.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API.Services.Auth
{
    public class AuthService(ApplicationDbContext context, IConfiguration configuration) : IAuthService 
    {
        public ApplicationDbContext Context { get; set; } = context;
        public IConfiguration Configuration { get; set; } = configuration;

        public async Task<ServiceResponse<string>> Login(string identifier, string password)
        {
            var response = new ServiceResponse<string>();

            try
            {
                var normalizedIdentifier = identifier.Trim().ToLowerInvariant();
                var matches = await Context.Users
                    .Where(user =>
                        user.Email.ToLower() == normalizedIdentifier ||
                        user.Nickname.ToLower() == normalizedIdentifier)
                    .Take(2)
                    .ToListAsync();

                if (matches.Count != 1)
                {
                    response.Success = false;
                    response.Message = "Credenciales incorrectas.";
                    return response;
                }

                var user = matches[0];
                if (!PasswordHasher.Verify(password, user.PasswordHash, user.PasswordSalt))
                {
                    response.Success = false;
                    response.Message = "Credenciales incorrectas.";
                    return response;
                }

                response.Data = CreateToken(user);
                response.Message = "Login ok.";
            }
            catch
            {
                response.Success = false;
                response.Message = "No se pudo validar el inicio de sesión.";
            }

            return response;
        }

        public async Task<ServiceResponse<int>> Register(User user, string password)
        {
            user.Email = user.Email.Trim();
            user.Nickname = user.Nickname.Trim().ToLowerInvariant();
            user.Role = user.Role.Trim();

            if (await UserExists(user.Email, user.Nickname))
            {
                return new ServiceResponse<int> { Success = false, Message = "El correo o nickname ya existe." };
            }

            if (!AppRoles.IsValid(user.Role))
            {
                return new ServiceResponse<int> { Success = false, Message = "El rol indicado no es válido." };
            }

            PasswordHasher.Create(password, out byte[] passwordHash, out byte[] passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;

            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            return new ServiceResponse<int>
            {
                Data = user.Id,
                Success = true,
                Message = "Usuario creado exitosamente.",
            };
        }

        public async Task<bool> UserExists(string email, string nickname)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var normalizedNickname = nickname.Trim().ToLowerInvariant();

            return await Context.Users.AnyAsync(user =>
                user.Email.ToLower() == normalizedEmail ||
                user.Nickname.ToLower() == normalizedEmail ||
                user.Email.ToLower() == normalizedNickname ||
                user.Nickname.ToLower() == normalizedNickname);
        }

        private string CreateToken(User user) 
        {
            List<Claim> claims =
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("nickname", user.Nickname)
            ];

            var mysecret = Configuration.GetSection("AppSettings:Token").Value!;

            if (string.IsNullOrEmpty(mysecret))
                throw new Exception("No se encontro la key principal en el appsettings...");

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8
                .GetBytes(mysecret));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var token = new JwtSecurityToken(
                    claims: claims,
                    expires: DateTime.UtcNow.AddDays(1),
                    signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;

        }


    }
}
