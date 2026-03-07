
namespace Shared.Dtos
{
    public class DocumentSettings
    {
        public int Consec { get; set; } 
        public string Prefijo { get; set; } = null!;
        public bool useSeparator { get; set; }
        public bool usePref { get; set; }
        public string NumberDocComplete { get; set; } = null!;
        public string CharacterSeparator { get; set; } = null!;
    }
}
