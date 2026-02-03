
using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Parametro
    {
        [Key]
        public Guid PropertyId { get; set; }
        public string Module { get; set; } = null!;
        public string PropertyName { get; set; } = null!;
        public string PropertyValue { get; set; } = null!;
        public bool Active { get; set; }
    }
}
