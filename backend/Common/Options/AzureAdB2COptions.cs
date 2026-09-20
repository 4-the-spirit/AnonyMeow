namespace AnonyMeow.Common.Options;

public class AzureAdB2COptions
{
    public const string SectionName = "AzureAdB2C";

    public required string Instance { get; set; }
    public required string ClientId { get; set; }
}
