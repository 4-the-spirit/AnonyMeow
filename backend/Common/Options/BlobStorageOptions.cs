namespace AnonyMeow.Common.Options;

public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}
