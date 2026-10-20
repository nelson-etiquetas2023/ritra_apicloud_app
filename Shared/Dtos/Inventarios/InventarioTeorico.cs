using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.Inventarios
{
    /// <summary>
    /// Inventario teórico: documento de referencia cargado en la web (importando Excel).
    /// Nunca modifica el stock; solo sirve para comparar contra un conteo físico.
    /// </summary>
    public class InventarioTeorico
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La carga inicial debe tener un número que la identifique.")]
        public string Numero { get; set; } = "";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string Comentario { get; set; } = "";

        public int Status { get; set; }

        [JsonIgnore]
        public string StatusTexto => Status switch
        {
            0 => "Creado",
            1 => "Actualizado",
            _ => Status.ToString()
        };

        public ICollection<InventarioTeoricoDetalle> Detalles { get; set; } = [];
    }

    public class InventarioTeoricoDetalle
    {
        [Key]
        public int Id { get; set; }

        public int InventarioTeoricoId { get; set; }

        [JsonIgnore]
        public InventarioTeorico? InventarioTeorico { get; set; }

        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double Cantidad { get; set; }
        public string Ubicacion { get; set; } = "";
        public decimal Costo { get; set; }
        public string Categoria { get; set; } = "";
        public string Unidad { get; set; } = "";
        public string Nota { get; set; } = "";

        [JsonIgnore]
        public decimal Subtotal => (decimal)Cantidad * Costo;
    }

    public class InventarioTeoricoImportResult
    {
        public bool Success { get; set; } = true;
        public int Inserted { get; set; }
        public int Skipped { get; set; }
        public List<RowError> Errors { get; set; } = [];
        public List<InventarioTeoricoDetalle> Detalles { get; set; } = [];
    }

    public class InventarioTeoricoSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public InventarioTeorico? Data { get; set; }
        public List<RowError> Errors { get; set; } = [];
    }

    public class RowError
    {
        public int Row { get; set; }
        public string Message { get; set; } = "";
    }
}
