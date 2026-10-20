using System.ComponentModel.DataAnnotations;

namespace Shared.Security
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Nickname { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public byte[]? PasswordHash { get; set; }
        public byte[]? PasswordSalt { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Now;

    }
}
