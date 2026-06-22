using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.AppMovil
{
    public class OrderPurchase
    {
        [Key]
        public string OrderId { get; set; } = null!;
        public DateTime OrderDate { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalCosto { get; set; }
        public bool Procesado { get; set; }
        public List<OrderPurchaseDetails> Items = [];
    }
}
