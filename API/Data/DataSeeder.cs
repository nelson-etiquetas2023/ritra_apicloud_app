using API.Services.Auth;
using Shared.Dtos;
using Shared.Security;

namespace API.Data
{
    public static class DataSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            const string defaultAdminEmail = "nelson.tecno2024@gmail.com";
            const string defaultAdminNickname = "npino";
            const string defaultAdminPassword = "Jossycar5%";

            // Identidades con las que la cuenta pudo quedar antes: se buscan
            // para corregir en caliente la base ya instalada en vez de crear
            // un segundo administrador.
            const string adminEmailAlterno = "admin@scanpro.local";
            const string adminNicknameAlterno = "admin";

            var admin = context.Users.FirstOrDefault(u =>
                            u.Nickname == defaultAdminNickname || u.Email == defaultAdminEmail)
                        ?? context.Users.FirstOrDefault(u =>
                            u.Nickname == adminNicknameAlterno || u.Email == adminEmailAlterno);

            if (admin is null)
            {
                PasswordHasher.Create(defaultAdminPassword, out var passwordHash, out var passwordSalt);

                context.Users.Add(new User
                {
                    Email = defaultAdminEmail,
                    Nickname = defaultAdminNickname,
                    UserName = "Administrador",
                    Role = AppRoles.Admin,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    DateCreated = DateTime.UtcNow
                });

                context.SaveChanges();
            }
            else
            {
                var changed = false;

                // El nickname tiene indice unico: no se renombra si otra cuenta
                // ya lo ocupa, para no abortar el seed completo.
                var nicknameEnUso = context.Users.Any(u =>
                    u.Id != admin.Id && u.Nickname == defaultAdminNickname);

                if (!nicknameEnUso &&
                    !string.Equals(admin.Nickname, defaultAdminNickname, StringComparison.OrdinalIgnoreCase))
                {
                    admin.Nickname = defaultAdminNickname;
                    changed = true;
                }

                var emailEnUso = context.Users.Any(u =>
                    u.Id != admin.Id && u.Email == defaultAdminEmail);

                if (!emailEnUso &&
                    !string.Equals(admin.Email, defaultAdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    admin.Email = defaultAdminEmail;
                    changed = true;
                }

                if (!string.Equals(admin.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
                {
                    admin.Role = AppRoles.Admin;
                    changed = true;
                }

                // Solo se regenera el hash si la contrasena actual no es la
                // esperada, para no invalidar el salt en cada arranque.
                if (!PasswordHasher.Verify(defaultAdminPassword, admin.PasswordHash, admin.PasswordSalt))
                {
                    PasswordHasher.Create(defaultAdminPassword, out var hash, out var salt);
                    admin.PasswordHash = hash;
                    admin.PasswordSalt = salt;
                    changed = true;
                }

                if (changed)
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
    }
}
