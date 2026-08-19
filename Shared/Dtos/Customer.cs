using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Customer
    {
        [Key]
        public int customer_id { get; set; }

        [StringLength(20)]
        public string CustomerCode { get; set; } = "";

        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [StringLength(150)]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "la direccion es obligatoria...")]
        public string Direccion { get; set; } = "";

        public string Registro_Fiscal { get; set; } = "";

        public string Telefono { get; set; } = "";

        public string Correo { get; set; } = "";

        public string Email { get; set; } = "";
    }
}