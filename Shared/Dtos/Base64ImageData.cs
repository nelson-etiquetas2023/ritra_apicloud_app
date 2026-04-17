namespace Shared.Dtos
{
    public class Base64ImageData
    {
        public string Base64Data { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public int ImageIndex { get; set; }
    }
}
