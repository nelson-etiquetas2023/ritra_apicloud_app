using System.ComponentModel.DataAnnotations;

namespace ScanProMovil.Data.Entities
{
    /// <summary>
    /// Correlativo local por tipo de documento (FIS para conteos físicos).
    /// Permite generar el siguiente número sin conexión y de forma atómica.
    /// </summary>
    public class ConsecutivoStockInit
    {
        [Key]
        public string Tipo_Documento { get; set; } = "FIS";
        public int UltimoNumero { get; set; }
    }
}
