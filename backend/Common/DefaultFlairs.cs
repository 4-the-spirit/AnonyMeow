namespace AnonyMeow.Common;

// Every community launches with this fixed set of tags, each with its own color so they're
// visually distinct at a glance. Extend NameToColorHex freely — nothing else in the codebase
// assumes exactly 3.
public static class DefaultFlairs
{
    public static readonly IReadOnlyDictionary<string, string> NameToColorHex = new Dictionary<string, string>
    {
        ["שאלה"] = "#4169E1", // question — royal blue
        ["דיון"] = "#22C55E", // discussion — green
        ["עזרה"] = "#EF4444", // help — red
        ["מחאה"] = "#F97316", // protest — orange
        ["הצהרה"] = "#A855F7", // statement — purple
        ["מידע"] = "#14B8A6", // information — teal
    };

    public static IReadOnlyList<string> Names { get; } = NameToColorHex.Keys.ToList();
}
