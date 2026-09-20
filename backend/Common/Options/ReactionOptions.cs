namespace AnonyMeow.Common.Options;

public class ReactionOptions
{
    public const string SectionName = "Reactions";

    public List<string> AllowedEmojis { get; set; } = ["👍", "👎", "❤️", "😂", "😮", "😢", "🔥"];
}
