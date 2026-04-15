using System.ComponentModel.DataAnnotations;

namespace Shared.Security
{
    public class UserRegister
    {
        [Required(ErrorMessage ="el correo es un valor necesario..."), EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage ="el nombre del usuario es necesario...")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "el rol del usuario es necesario...")]
        public string Role { get; set; } = string.Empty;

        [Required(ErrorMessage ="el password es un valor necesario...")]
        public string Password { get; set; } = string.Empty;
        
        [Compare("Password", ErrorMessage ="no coinciden el password...")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
