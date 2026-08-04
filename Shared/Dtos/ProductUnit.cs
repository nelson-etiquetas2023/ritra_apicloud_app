using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;

public class ProductUnit
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la unidad es obligatorio.")]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(150)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
