using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Almacen
    {
        [Key]
        public int almacen_id { get; set; }

        [StringLength(20)]
        public string almacen_code { get; set; } = "";

        [Required(ErrorMessage = "El nombre del almacén es obligatorio.")]
        [StringLength(100)]
        public string almacen_name { get; set; } = "";

        [StringLength(250)]
        public string descripcion { get; set; } = "";
    }
}
