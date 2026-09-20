using System.Text.RegularExpressions;
using AnonyMeow.Common.Options;
using AnonyMeow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services.SpamDetection;

// Flags submissions linking to a domain on the configured blocklist. No new NuGet package —
// System.Text.RegularExpressions is already used elsewhere in the codebase (PII detectors).
public partial class LinkSpamHeuristic(IOptions<SpamDetectionOptions> options) : ISpamHeuristic
{
    public SpamFlagReason Reason => SpamFlagReason.LinkSpam;

    public Task<bool> IsMatchAsync(SpamDetectionContext context, CancellationToken cancellationToken = default)
    {
        var blockedDomains = options.Value.BlockedLinkDomains;
        if (blockedDomains.Count == 0 || string.IsNullOrWhiteSpace(context.Text))
        {
            return Task.FromResult(false);
        }

        foreach (Match match in UrlPattern().Matches(context.Text))
        {
            if (!Uri.TryCreate(match.Value, UriKind.Absolute, out var uri))
            {
                continue;
            }

            var host = uri.Host.ToLowerInvariant();
            if (blockedDomains.Any(domain => host == domain || host.EndsWith("." + domain)))
            {
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlPattern();
}
