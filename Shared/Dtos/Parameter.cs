using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos
{
    public class Parameter
    {
        [Key]
        public int  ParameterId { get; set; }
        public string Name { get; set; } = null!;
        public string Module { get; set; } = null!;
        public string Value1 { get; set; } = null!;
        public string Value2 { get; set; } = null!;
        public string Value3 { get; set; } = null!;
        public string Value4 { get; set; } = null!;
        public string Value5 { get; set; } = null!;
        public string Value6 { get; set; } = null!;
        public string Value7 { get; set; } = null!;
        public string Value8 { get; set; } = null!;
        public string Value9 { get; set; } = null!;
        public string Value10 { get; set; } = null!;

    }
}
