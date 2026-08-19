namespace Shared.Dtos
{
    public class CustomerImportResult
    {
        public bool Success { get; set; } = true;
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<CustomerImportError> Errors { get; set; } = [];
    }

    public class CustomerImportError
    {
        public int Row { get; set; }
        public string Message { get; set; } = "";
    }
}