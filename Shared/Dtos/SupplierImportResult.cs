namespace Shared.Dtos
{
    public class SupplierImportResult
    {
        public bool Success { get; set; } = true;
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<SupplierImportError> Errors { get; set; } = [];
    }

    public class SupplierImportError
    {
        public int Row { get; set; }
        public string Message { get; set; } = "";
    }
}
