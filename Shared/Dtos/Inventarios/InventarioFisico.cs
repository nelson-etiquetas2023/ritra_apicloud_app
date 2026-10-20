using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.Inventarios
{
    public class InventarioFisico
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El documento físico debe tener un número que lo identifique.")]
        public string Numero { get; set; } = "";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        /// <summary>Documento teórico con el que se concilió este conteo. Solo es referencia.</summary>
        public string DocumentoTeorico { get; set; } = "";

        public string Comentario { get; set; } = "";

        public int Status { get; set; }

        /// <summary>Momento en que el conteo se aplicó al inventario de productos.</summary>
        public DateTime? FechaAplicacion { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string StatusTexto => Status switch
        {
            0 => "Creado",
            1 => "Actualizado",
            2 => "Sincronizado",
            4 => "Aplicado al inventario",
            _ => Status.ToString()
        };

        public ICollection<InventarioFisicoDetalle> Detalles { get; set; } = [];
    }

    public class InventarioFisicoDetalle
    {
        [Key]
        public int Id { get; set; }

        public int InventarioFisicoId { get; set; }

        [JsonIgnore]
        public InventarioFisico? InventarioFisico { get; set; }

        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double CantidadFisica { get; set; }
        public string Ubicacion { get; set; } = "";
        public decimal Costo { get; set; }
        public string Categoria { get; set; } = "";
        public string Unidad { get; set; } = "";
        public string Nota { get; set; } = "";
        public bool Procesado { get; set; }
        public DateTime? FechaProcesado { get; set; }

        [JsonIgnore]
        public decimal Subtotal => (decimal)CantidadFisica * Costo;
    }

    public class FiscalSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public InventarioFisico? Data { get; set; }
        public List<RowError> Errors { get; set; } = [];
    }
}
