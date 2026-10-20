using Shared.Dtos;
using Shared.Security;
using System.Security.Cryptography;

namespace API.Data
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            const string defaultAdminEmail = "nelson.tecno2024@gmail.com";
            const string defaultAdminPassword = "Jossycar5%";
            const string defaultAdminNickname = "npino";

            var defaultAdmin = context.Users.FirstOrDefault(u => u.Email == defaultAdminEmail);
            if (defaultAdmin is null)
            {
                CreatePasswordHash(defaultAdminPassword, out var passwordHash, out var passwordSalt);

                context.Users.Add(new User
                {
                    Email = defaultAdminEmail,
                    Nickname = defaultAdminNickname,
                    UserName = "Nelson",
                    Role = AppRoles.Admin,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    DateCreated = DateTime.UtcNow
                });

                context.SaveChanges();
            }
            else if (string.IsNullOrWhiteSpace(defaultAdmin.Nickname))
            {
                defaultAdmin.Nickname = defaultAdminNickname;
                context.SaveChanges();
            }

            if (!context.Parametros.Any(p => p.Name == "Configuracion del consecutivo de inventario"))
            {
                context.Parametros.Add(new Parameter
                {
                    Name = "Configuracion del consecutivo de inventario",
                    Module = "DocumentSetting",
                    Value1 = "10000",
                    Value2 = "DOC",
                    Value3 = "true",
                    Value4 = "true",
                    Value5 = "#",
                    Value6 = "",
                    Value7 = "",
                    Value8 = "",
                    Value9 = "",
                    Value10 = ""
                });
                context.SaveChanges();
            }
        }

        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512();
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }
}
