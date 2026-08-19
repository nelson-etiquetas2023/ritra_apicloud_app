using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Dtos;

public class Location
{
    [Key]
    public int LocationId { get; set; }

    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(30)]
    public string Code { get; set; } = string.Empty;

    [StringLength(50)]
    public string Barcode { get; set; } = string.Empty;

    [StringLength(20)]
    public string BarcodeType { get; set; } = string.Empty;

    [StringLength(20)]
    public string Zone { get; set; } = string.Empty;

    [StringLength(10)]
    public string Aisle { get; set; } = string.Empty;

    [StringLength(10)]
    public string Rack { get; set; } = string.Empty;

    [StringLength(10)]
    public string Level { get; set; } = string.Empty;

    [StringLength(10)]
    public string Position { get; set; } = string.Empty;

    public decimal Capacity { get; set; }

    public decimal CurrentCapacity { get; set; }

    public bool AllowMixedProducts { get; set; }

    public byte Status { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [NotMapped]
    public string? WarehouseName { get; set; }
}
