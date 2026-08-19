using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [StringLength(20)]
        public string SupplierCode { get; set; } = "";

        [Required(ErrorMessage = "El nombre del proveedor es obligatorio.")]
        [StringLength(150)]
        public string SupplierName { get; set; } = "";

        [StringLength(20)]
        public string Ruc { get; set; } = "";

        [StringLength(100)]
        public string ContactName { get; set; } = "";

        [StringLength(30)]
        public string Phone { get; set; } = "";

        [StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(250)]
        public string Address { get; set; } = "";

        [StringLength(100)]
        public string City { get; set; } = "";

        [StringLength(100)]
        public string Country { get; set; } = "";

        [StringLength(150)]
        public string Website { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
