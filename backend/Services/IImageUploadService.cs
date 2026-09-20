namespace AnonyMeow.Services;

public interface IImageUploadService
{
    // Throws BlobStorageNotConfiguredException while BlobStorageOptions is unset (placeholder
    // config only — real provisioning is a Phase 1 open item pending user confirmation).
    Task<(string UploadUrl, string BlobUrl)> CreateUploadSasAsync(Guid userId, CancellationToken cancellationToken = default);

    // Verifies blobUrl belongs to ownerId (path-prefix ownership check), exists, is within the
    // configured max size, and is a recognized image format via a magic-byte check — the
    // client-supplied Content-Type is never trusted, since it's fully attacker-controlled. On any
    // failure the blob is deleted before the corresponding ApiException is thrown, so a
    // rejected/oversized/spoofed upload doesn't linger in storage.
    Task ValidateImageAsync(string blobUrl, Guid ownerId, CancellationToken cancellationToken = default);
}
