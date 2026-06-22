using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos.AppMovil
{
    public class OrderPurchaseDetails
    {
        [Key]
        public int OrderId { get; set; }
        public string ProductId { get; set; } = null!;
        public double Quantity { get; set; }
        public decimal Costo { get; set; }
        public string Location { get; set; } = null!;
    }
}
