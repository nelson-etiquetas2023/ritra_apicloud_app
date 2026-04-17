namespace Shared.Dtos
{
    public class CreateProductWithImagesRequest
    {
        public Product Product { get; set; } = new();
        public List<Base64ImageData> Images { get; set; } = new();
    }
}
