using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string Login { get; set; } = null!;
        public string User_Name { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Active { get; set; } = null!;
    }
}
