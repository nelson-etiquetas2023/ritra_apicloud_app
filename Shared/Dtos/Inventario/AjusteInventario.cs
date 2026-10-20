using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Dtos.Inventario
{
    public static class MotivoAjuste
    {
        public const string Inicial = "INICIAL";
    }

    // Bitácora de ajustes de inventario (carga inicial, y a futuro conteos).
    // Cada fila deja rastro de quién/cuándo/antes/después para auditoría.
    public class AjusteInventario
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public string Usuario { get; set; } = "";

        public string Lote { get; set; } = "";

        public int ProductoId { get; set; }

        public string Codigo { get; set; } = "";

        public double StockAnterior { get; set; }

        public double StockNuevo { get; set; }

        public string Motivo { get; set; } = MotivoAjuste.Inicial;

        public string Comentario { get; set; } = "";

        [NotMapped]
        public string ProductoNombre { get; set; } = "";
    }

    public class StockInicialLinea
    {
        public int Row { get; set; }
        public string Codigo { get; set; } = "";
        public double StockInicial { get; set; }
    }

    public class StockInicialError
    {
        public int Row { get; set; }
        public string Message { get; set; } = "";
    }

    public class StockInicialResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Lote { get; set; } = "";
        public int Aplicados { get; set; }
        public int Omitidos { get; set; }
        public List<StockInicialError> Errors { get; set; } = [];
    }

    public class CargaInicialResumen
    {
        public string Lote { get; set; } = "";
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "";
        public int Productos { get; set; }
        public string Comentario { get; set; } = "";
    }

    public class CargaInicialUpdate
    {
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = "";
    }
}
