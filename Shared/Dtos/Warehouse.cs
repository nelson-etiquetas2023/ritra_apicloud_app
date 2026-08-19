using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;

public class Warehouse
{
    [Key]
    public int WarehouseId { get; set; }

    [Required(ErrorMessage = "El nombre del almacén es obligatorio.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
