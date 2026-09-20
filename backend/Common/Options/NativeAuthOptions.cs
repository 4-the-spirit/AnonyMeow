namespace AnonyMeow.Common.Options;

// Deliberately not bound from its own appsettings section — Program.cs derives both fields from
// the already-configured AzureAdB2COptions (same CIAM app registration), so no new secrets/Bicep
// params/GitHub Actions changes are needed to support native authentication.
public class NativeAuthOptions
{
    public required string BaseUrl { get; set; }
    public required string ClientId { get; set; }
}
