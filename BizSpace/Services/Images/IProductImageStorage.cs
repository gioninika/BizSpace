namespace BizSpace.Services.Images;

public sealed record ProductImageUploadResult(
    string PublicId,
    string SecureUrl,
    string? Format,
    int? Width,
    int? Height);

public enum ImageEntityType
{
    Product,
    Category
}

public interface IProductImageStorage
{
    Task<ProductImageUploadResult> UploadAsync(
        Stream content,
        string contentType,
        ImageEntityType entityType,
        int entityId,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken = default);

    string? TryGetPublicIdFromSecureUrl(string? secureUrl);
}

public sealed class ImageStorageNotConfiguredException : Exception
{
    public ImageStorageNotConfiguredException() : base("Image storage is not configured.") { }
}

public sealed class ImageStorageException : Exception
{
    public ImageStorageException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
