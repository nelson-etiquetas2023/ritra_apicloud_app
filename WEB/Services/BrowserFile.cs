using Microsoft.AspNetCore.Components.Forms;

namespace WEB.Services;

public class BrowserFile : IBrowserFile
{
    private readonly string _base64Data;

    public string Name { get; }
    public DateTimeOffset LastModified { get; }
    public long Size { get; }
    public string ContentType { get; }

    public BrowserFile(string name, long size, string contentType, string base64Data)
    {
        Name = name;
        Size = size;
        ContentType = contentType;
        _base64Data = base64Data;
        LastModified = DateTimeOffset.Now;
    }

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
    {
        return new MemoryStream(DecodeBase64(_base64Data));
    }

    private static byte[] DecodeBase64(string data)
    {
        var comma = data.IndexOf(',');
        if (comma > 0 && data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            data = data.Substring(comma + 1);
        return Convert.FromBase64String(data);
    }
}
