namespace AnonyMeow.Common.Options;

public class PostImageOptions
{
    public const string SectionName = "PostImages";

    public int MaxImageCount { get; set; } = 6;
    public long MaxImageSizeBytes { get; set; } = 8 * 1024 * 1024;
}
