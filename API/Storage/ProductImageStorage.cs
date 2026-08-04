namespace API.Storage;

public static class ProductImageStorage
{
    public const string ConfigurationKey = "Storage:ProductImagesPath";

    public static string GetPath(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration[ConfigurationKey];

        return string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "uploads")
            : Path.GetFullPath(configuredPath);
    }
}
