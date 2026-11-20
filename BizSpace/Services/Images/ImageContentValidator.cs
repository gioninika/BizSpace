namespace BizSpace.Services.Images;

public static class ImageContentValidator
{
    public const long MaxImageBytes = 8 * 1024 * 1024;

    public static async Task<string> DetectContentTypeAsync(
        Stream content,
        string declaredContentType,
        CancellationToken cancellationToken = default)
    {
        if (!content.CanSeek)
            throw new ArgumentException("The uploaded image stream could not be read.");

        var initialPosition = content.Position;
        var header = new byte[12];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var count = await content.ReadAsync(header.AsMemory(bytesRead), cancellationToken);
            if (count == 0) break;
            bytesRead += count;
        }
        content.Position = initialPosition;

        string? detectedContentType = null;
        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
            detectedContentType = "image/jpeg";
        else if (bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            detectedContentType = "image/png";
        else if (bytesRead >= 12 &&
                 header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                 header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            detectedContentType = "image/webp";

        if (detectedContentType is null)
            throw new ArgumentException("Only valid JPEG, PNG, and WebP images are supported by the BizSpace upload endpoint.");

        var normalizedDeclaredType = declaredContentType.Split(';', 2)[0].Trim();
        var isJpegAlias = detectedContentType == "image/jpeg" &&
            string.Equals(normalizedDeclaredType, "image/jpg", StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(normalizedDeclaredType) &&
            normalizedDeclaredType != "application/octet-stream" &&
            !isJpegAlias &&
            !string.Equals(normalizedDeclaredType, detectedContentType, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The uploaded file content type does not match its image format.");

        return detectedContentType;
    }
}
