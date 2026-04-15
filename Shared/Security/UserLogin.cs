using System.ComponentModel.DataAnnotations;

namespace Shared.Security
{
    public class UserLogin
    {
        [Required(ErrorMessage ="el correo electronico es un campo requerido..."),EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
