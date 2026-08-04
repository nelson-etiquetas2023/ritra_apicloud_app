namespace Shared.Dtos
{
    public class ProductImportResult
    {
        public bool Success { get; set; } = true;
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<ProductImportError> Errors { get; set; } = [];
    }

    public class ProductImportError
    {
        public int Row { get; set; }
        public string Message { get; set; } = "";
    }
}
