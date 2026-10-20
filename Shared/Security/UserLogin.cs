using System.ComponentModel.DataAnnotations;

namespace Shared.Security
{
    public class UserLogin
    {
        [StringLength(256)]
        public string Identifier { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [Required]
        [StringLength(256)]
        public string Password { get; set; } = string.Empty;

        public string ResolveIdentifier()
        {
            return string.IsNullOrWhiteSpace(Identifier)
                ? Email?.Trim() ?? string.Empty
                : Identifier.Trim();
        }
    }
}
