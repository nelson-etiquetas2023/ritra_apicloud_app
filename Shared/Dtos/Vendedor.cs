using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Vendedor
    {
        [Key]
        public int vendedor_id { get; set; }

        [StringLength(20)]
        public string vendedor_code { get; set; } = "";

        [Required(ErrorMessage = "El nombre del vendedor es obligatorio.")]
        [StringLength(100)]
        public string vendedor_name { get; set; } = "";

        [StringLength(20)]
        public string telefono { get; set; } = "";

        [StringLength(100)]
        public string email { get; set; } = "";
    }
}
