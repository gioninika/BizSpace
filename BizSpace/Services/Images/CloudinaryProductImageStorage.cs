using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BizSpace.Exceptions;
using Microsoft.Extensions.Options;

namespace BizSpace.Services.Images;

public sealed class CloudinaryProductImageStorage(
    HttpClient httpClient,
    IOptions<CloudinaryOptions> options,
    ILogger<CloudinaryProductImageStorage> logger) : IProductImageStorage
{
    private readonly CloudinaryOptions _options = options.Value;

    public async Task<ProductImageUploadResult> UploadAsync(
        Stream content,
        string contentType,
        ImageEntityType entityType,
        int entityId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var folder = entityType switch
        {
            ImageEntityType.Product => "products",
            ImageEntityType.Category => "categories",
            _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unsupported image owner type.")
        };
        var publicId = $"{folder}/{entityId}/{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var parametersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp
        };

        using var request = new MultipartFormDataContent();
        var filePart = new StreamContent(content);
        filePart.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        var extension = contentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty
        };
        request.Add(filePart, "file", $"product-image{extension}");
        request.Add(new StringContent(_options.ApiKey), "api_key");
        request.Add(new StringContent(timestamp), "timestamp");
        request.Add(new StringContent(publicId), "public_id");
        request.Add(new StringContent(CreateSignature(parametersToSign)), "signature");

        var endpoint = new Uri($"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(_options.CloudName)}/image/upload");
        using var response = await SendAsync(() => httpClient.PostAsync(endpoint, request, cancellationToken), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Cloudinary rejected a product image upload with status {StatusCode}. Provider detail: {ProviderDetail}",
                (int)response.StatusCode,
                ReadProviderDetail(response, body));
            throw new ImageStorageException("The image provider could not accept the upload.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var returnedPublicId = root.GetProperty("public_id").GetString();
            var secureUrl = root.GetProperty("secure_url").GetString();
            if (string.IsNullOrWhiteSpace(returnedPublicId) ||
                !Uri.TryCreate(secureUrl, UriKind.Absolute, out var parsedUrl) ||
                parsedUrl.Scheme != Uri.UriSchemeHttps)
                throw new ImageStorageException("The image provider returned an invalid upload result.");

            return new ProductImageUploadResult(
                returnedPublicId,
                parsedUrl.AbsoluteUri,
                ReadString(root, "format"),
                ReadInt(root, "width"),
                ReadInt(root, "height"));
        }
        catch (JsonException exception)
        {
            throw new ImageStorageException("The image provider returned an invalid upload result.", exception);
        }
        catch (KeyNotFoundException exception)
        {
            throw new ImageStorageException("The image provider returned an invalid upload result.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ImageStorageException("The image provider returned an invalid upload result.", exception);
        }
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var parametersToSign = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp
        };
        var form = new Dictionary<string, string>
        {
            ["api_key"] = _options.ApiKey,
            ["public_id"] = publicId,
            ["timestamp"] = timestamp,
            ["signature"] = CreateSignature(parametersToSign)
        };
        var endpoint = new Uri($"https://api.cloudinary.com/v1_1/{Uri.EscapeDataString(_options.CloudName)}/image/destroy");
        using var request = new FormUrlEncodedContent(form);
        using var response = await SendAsync(
            () => httpClient.PostAsync(endpoint, request, cancellationToken), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Cloudinary rejected a product image deletion with status {StatusCode}. Provider detail: {ProviderDetail}",
                (int)response.StatusCode,
                ReadProviderDetail(response, body));
            throw new ImageStorageException("The image provider could not delete the image.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var result = document.RootElement.TryGetProperty("result", out var resultElement)
                ? resultElement.GetString()
                : null;
            if (result is not "ok" and not "not found")
                throw new ImageStorageException("The image provider could not delete the image.");
        }
        catch (JsonException exception)
        {
            throw new ImageStorageException("The image provider returned an invalid deletion result.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new ImageStorageException("The image provider returned an invalid deletion result.", exception);
        }
    }

    public string? TryGetPublicIdFromSecureUrl(string? secureUrl)
    {
        if (string.IsNullOrWhiteSpace(secureUrl) || string.IsNullOrWhiteSpace(_options.CloudName) ||
            !Uri.TryCreate(secureUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "res.cloudinary.com", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.UnescapeDataString)
            .ToArray();
        if (path.Length < 5 ||
            !string.Equals(path[0], _options.CloudName, StringComparison.Ordinal) ||
            !string.Equals(path[1], "image", StringComparison.Ordinal) ||
            !string.Equals(path[2], "upload", StringComparison.Ordinal))
            return null;

        var versionIndex = Array.FindIndex(path, 3, segment =>
            segment.Length > 1 && segment[0] == 'v' && segment.AsSpan(1).IndexOfAnyExceptInRange('0', '9') < 0);
        if (versionIndex < 0 || versionIndex == path.Length - 1)
            return null;

        var publicIdSegments = path[(versionIndex + 1)..];
        var finalSegment = publicIdSegments[^1];
        var extensionIndex = finalSegment.LastIndexOf('.');
        if (extensionIndex > 0)
            publicIdSegments[^1] = finalSegment[..extensionIndex];

        var publicId = string.Join('/', publicIdSegments);
        return string.IsNullOrWhiteSpace(publicId) ? null : publicId;
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (HttpRequestException exception)
        {
            throw new ImageStorageException("The image provider could not be reached.", exception);
        }
        catch (TaskCanceledException exception)
        {
            throw new ImageStorageException("The image provider request timed out.", exception);
        }
    }

    private string CreateSignature(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var values = string.Join("&", parameters.Select(parameter => $"{parameter.Key}={parameter.Value}"));
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(values + _options.ApiSecret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? ReadProviderDetail(HttpResponseMessage response, string body)
    {
        var detail = response.Headers.TryGetValues("X-Cld-Error", out var headerValues)
            ? headerValues.FirstOrDefault()
            : null;

        if (string.IsNullOrWhiteSpace(detail))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String)
                    detail = message.GetString();
            }
            catch (JsonException)
            {
                // Provider response bodies are not guaranteed to be JSON.
            }
        }

        if (string.IsNullOrWhiteSpace(detail))
            return null;

        var safeDetail = new string(detail.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return safeDetail.Length <= 300 ? safeDetail : safeDetail[..300];
    }

    private void EnsureConfigured()
    {
        if (!_options.IsConfigured)
            throw new ImageStorageNotConfiguredException();
    }

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : null;
}
