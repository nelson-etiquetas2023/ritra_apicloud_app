using Microsoft.AspNetCore.Components.Forms;

namespace WEB.Services.Upload
{
    public class BrowserFile : IBrowserFile
    {
        private readonly byte[] _bytes;

        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public long Size { get; }
        public string ContentType { get; }

        public BrowserFile(string name, long size, string contentType, string base64Data)
        {
            Name = name;
            Size = size;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            LastModified = DateTimeOffset.UtcNow;
            _bytes = Convert.FromBase64String(base64Data);
        }

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            if (_bytes.Length > maxAllowedSize)
                throw new IOException($"El archivo excede el tamaño máximo permitido de {maxAllowedSize} bytes.");

            return new MemoryStream(_bytes, writable: false);
        }
    }
}
