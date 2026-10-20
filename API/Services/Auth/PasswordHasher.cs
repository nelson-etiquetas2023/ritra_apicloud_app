using System.Security.Cryptography;
using System.Text;

namespace API.Services.Auth
{
    /// <summary>
    /// Hash de contrasenas con HMACSHA512 y un salt aleatorio por usuario.
    /// </summary>
    public static class PasswordHasher
    {
        public static void Create(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }

        public static bool Verify(string password, byte[]? passwordHash, byte[]? passwordSalt)
        {
            if (passwordHash is null || passwordSalt is null)
                return false;

            using var hmac = new HMACSHA512(passwordSalt);
            var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return CryptographicOperations.FixedTimeEquals(computed, passwordHash);
        }
    }
}
