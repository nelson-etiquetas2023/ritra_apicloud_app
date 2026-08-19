using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Enterprise
    {
        [Key]
        public int enterprise_id { get; set; }

        public byte[]? Logo { get; set; }

        public string LogoContentType { get; set; } = "image/png";

        public string Codigo_Empresa { get; set; } = "";

        [Required(ErrorMessage = "el nombre de la empresa es obligatorio...")]
        public string Nombre_Empresa { get; set; } = "";

        public string Tipo_Empresa { get; set; } = "";

        public string Registro_Fiscal { get; set; } = "";

        public string Direccion { get; set; } = "";

        public string Telefono { get; set; } = "";

        public string Correo { get; set; } = "";

        public double Latitud { get; set; } = 0;

        public double Longitud { get; set; } = 0;

        public string Persona_Contacto { get; set; } = "";
    }
}