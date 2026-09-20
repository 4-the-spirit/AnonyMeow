namespace AnonyMeow.Common.Options;

public class SpamDetectionOptions
{
    public const string SectionName = "SpamDetection";

    // "New account" window for the posting-frequency heuristic — accounts older than this are
    // never flagged by it, regardless of posting rate.
    public int NewAccountWindowMinutes { get; set; } = 60;

    // Within NewAccountWindowMinutes of signup, more than this many Post/Comment/Message
    // submissions inside PostingFrequencyWindowMinutes flags the account. Starts conservative,
    // per the parent plan's "concrete thresholds start conservative, iterate".
    public int MaxSubmissionsForNewAccounts { get; set; } = 5;
    public int PostingFrequencyWindowMinutes { get; set; } = 10;

    // How far back to look for identical prior submissions by the same author.
    public int DuplicateContentLookbackHours { get; set; } = 24;

    // Starter blocklist — empty by default; real curation is ops work via configuration, not a
    // code change, per CLAUDE.md's "configuration differences go through configuration".
    public HashSet<string> BlockedLinkDomains { get; set; } = [];
}
