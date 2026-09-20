using AnonyMeow.Common.Exceptions;
using AnonyMeow.Common.Options;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services;

public class ImageUploadService(
    IOptions<BlobStorageOptions> blobOptions, IOptions<PostImageOptions> imageOptions) : IImageUploadService
{
    private static readonly TimeSpan SasLifetime = TimeSpan.FromMinutes(10);
    private const int SignatureProbeLength = 12;

    // Simple contiguous-prefix signatures. WEBP ("RIFF"...."WEBP") isn't contiguous, so it's
    // checked separately in HasValidImageSignatureAsync.
    private static readonly byte[][] ImageSignatures =
    [
        [0xFF, 0xD8, 0xFF],                                     // JPEG
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A],       // PNG
        [0x47, 0x49, 0x46, 0x38]                                // GIF87a / GIF89a
    ];

    public Task<(string UploadUrl, string BlobUrl)> CreateUploadSasAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var blobClient = containerClient.GetBlobClient($"{userId}/{Guid.NewGuid()}");

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = blobOptions.Value.ContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(SasLifetime)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Write | BlobSasPermissions.Create);

        var uploadUrl = blobClient.GenerateSasUri(sasBuilder).ToString();
        return Task.FromResult((uploadUrl, blobClient.Uri.ToString()));
    }

    public async Task ValidateImageAsync(string blobUrl, Guid ownerId, CancellationToken cancellationToken = default)
    {
        var containerClient = GetContainerClient();
        var blobName = ParseBlobName(blobUrl, containerClient.Name);

        // Blobs are minted at "{userId}/{guid}" (see CreateUploadSasAsync) — anything else means
        // the caller is referencing a blob it never had a SAS for (its own under a guessed path,
        // someone else's, or a forged URL).
        if (!blobName.StartsWith($"{ownerId}/", StringComparison.Ordinal))
        {
            throw new ImageOwnershipMismatchException(blobUrl);
        }

        var blobClient = containerClient.GetBlobClient(blobName);

        BlobProperties properties;
        try
        {
            properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Matches the pre-existing "not found or too large" contract callers already expect.
            throw new PostImageTooLargeException(blobUrl, imageOptions.Value.MaxImageSizeBytes);
        }

        if (properties.ContentLength > imageOptions.Value.MaxImageSizeBytes)
        {
            await DeleteQuietlyAsync(blobClient, cancellationToken);
            throw new PostImageTooLargeException(blobUrl, imageOptions.Value.MaxImageSizeBytes);
        }

        if (!await HasValidImageSignatureAsync(blobClient, cancellationToken))
        {
            await DeleteQuietlyAsync(blobClient, cancellationToken);
            throw new UnsupportedImageContentTypeException(blobUrl);
        }
    }

    private BlobContainerClient GetContainerClient()
    {
        var options = blobOptions.Value;
        if (string.IsNullOrEmpty(options.ConnectionString) || string.IsNullOrEmpty(options.ContainerName))
        {
            throw new BlobStorageNotConfiguredException();
        }

        var blobServiceClient = new BlobServiceClient(options.ConnectionString);
        return blobServiceClient.GetBlobContainerClient(options.ContainerName);
    }

    private static string ParseBlobName(string blobUrl, string containerName)
    {
        var containerPrefix = $"/{containerName}/";
        var path = new Uri(blobUrl).AbsolutePath;
        return path.StartsWith(containerPrefix, StringComparison.Ordinal)
            ? path[containerPrefix.Length..]
            : path.TrimStart('/');
    }

    private static async Task<bool> HasValidImageSignatureAsync(BlobClient blobClient, CancellationToken cancellationToken)
    {
        byte[] head;
        try
        {
            var range = new HttpRange(0, SignatureProbeLength);
            var response = await blobClient.DownloadStreamingAsync(
                new BlobDownloadOptions { Range = range }, cancellationToken);
            await using var stream = response.Value.Content;
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            head = buffer.ToArray();
        }
        catch (RequestFailedException)
        {
            return false;
        }

        if (head.Length >= 12 &&
            head[0] == 0x52 && head[1] == 0x49 && head[2] == 0x46 && head[3] == 0x46 &&
            head[8] == 0x57 && head[9] == 0x45 && head[10] == 0x42 && head[11] == 0x50)
        {
            return true; // WEBP: "RIFF"[4-byte size]"WEBP"
        }

        return ImageSignatures.Any(signature =>
            head.Length >= signature.Length && head.AsSpan(0, signature.Length).SequenceEqual(signature));
    }

    private static async Task DeleteQuietlyAsync(BlobClient blobClient, CancellationToken cancellationToken)
    {
        try
        {
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (RequestFailedException)
        {
            // Best-effort cleanup — a delete failure shouldn't mask the validation failure the
            // caller is about to be told about.
        }
    }
}
