using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;

public class Category
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
