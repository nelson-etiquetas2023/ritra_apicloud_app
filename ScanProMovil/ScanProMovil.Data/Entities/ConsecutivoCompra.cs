using System.ComponentModel.DataAnnotations;

namespace ScanProMovil.Data.Entities
{
    public class ConsecutivoCompra
    {
        [Key]
        public string Tipo_Documento { get; set; } = "PO";
        public int UltimoNumero { get; set; }
    }
}