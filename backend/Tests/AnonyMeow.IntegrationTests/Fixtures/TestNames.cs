using AnonyMeow.Dtos.Flairs;

namespace AnonyMeow.IntegrationTests.Fixtures;

/// <summary>Mirrors CommunityEndpoints.cs: community names must match ^[א-ת0-9_ -]{3,30}$.</summary>
public static class TestNames
{
    private static readonly char[] HebrewDigits = "אבגדהוזחטיכלמנסע".ToCharArray();

    /// <summary>An optional custom flair set (in addition to the server-seeded default tags)
    /// tests that don't care about flairs specifically can reuse.</summary>
    public static IReadOnlyList<CreateFlairRequest> DefaultFlairs { get; } =
        [new CreateFlairRequest("General", "#22C55E")];

    /// <summary>Icon/banner are mandatory on CreateCommunityRequest — placeholder URLs for tests
    /// that don't care about the images specifically.</summary>
    public const string DefaultIconUrl = "https://example.com/icon.png";
    public const string DefaultBannerUrl = "https://example.com/banner.png";

    public static string UniqueCommunityName(int length = 15)
    {
        var hex = Guid.NewGuid().ToString("N");
        while (hex.Length < length)
        {
            hex += Guid.NewGuid().ToString("N");
        }

        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = HebrewDigits[Convert.ToInt32(hex[i].ToString(), 16)];
        }

        return new string(chars);
    }
}
