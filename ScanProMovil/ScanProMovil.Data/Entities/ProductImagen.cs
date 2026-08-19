using Microsoft.Maui.Storage;

namespace ScanProMovil.Data.Entities
{
    public class ProductImagen
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ImageIndex { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Url
        {
            get
            {
                var baseUrl = Preferences.Get("ApiBaseUrl", string.Empty);
                return $"{baseUrl}/api/Products/getproductimage/{Id}";
            }
        }
        public string Base64 { get; set; } = string.Empty;


    }
}
