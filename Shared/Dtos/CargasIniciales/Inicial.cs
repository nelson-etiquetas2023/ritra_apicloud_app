using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shared.Dtos.CargasIniciales
{
    public class Inicial
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "La carga inicial debe tener un número que la identifique.")]
        public string Numero { get; set; } = "";

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public string Comentario { get; set; } = "";

        public int Status { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string StatusTexto => Status switch
        {
            0 => "Creado",
            1 => "Actualizado",
            2 => "Sincronizado",
            4 => "Procesado",
            5 => "Transacción Fallida",
            _ => Status.ToString()
        };

        public ICollection<DetalleInicial> Detalles { get; set; } = [];
    }

    public class DetalleInicial
    {
        [Key]
        public int Id { get; set; }

        public int InicialId { get; set; }

        [JsonIgnore]
        public Inicial? Inicial { get; set; }

        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Cantidad { get; set; }
        public int CantidadFisica { get; set; }
        public string Ubicacion { get; set; } = "";
        public decimal Costo { get; set; }
        public string Categoria { get; set; } = "";
        public string Unidad { get; set; } = "";
        public string Nota { get; set; } = "";
        public bool Procesado { get; set; }
        public DateTime? FechaProcesado { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int Diferencia => CantidadFisica - Cantidad;

        [System.Text.Json.Serialization.JsonIgnore]
        public decimal Subtotal => Cantidad * Costo;
    }

    public class CargaInicialImportResult
    {
        public bool Success { get; set; } = true;
        public int Inserted { get; set; }
        public int Skipped { get; set; }
        public List<RowError> Errors { get; set; } = [];
        public List<DetalleInicial> Detalles { get; set; } = [];
    }

    public class CargaInicialSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public Inicial? Data { get; set; }
        public List<RowError> Errors { get; set; } = [];
    }

    public class RowError
    {
        public int Row { get; set; }
        public string Message { get; set; } = "";
    }
}