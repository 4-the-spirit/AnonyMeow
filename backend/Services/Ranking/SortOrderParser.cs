using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.Ranking;

public static class SortOrderParser
{
    // Lenient by design: an unrecognized/missing ?sort= value falls back to New rather than 400ing.
    public static SortOrder Parse(string? sort) =>
        Enum.TryParse<SortOrder>(sort, ignoreCase: true, out var parsed) ? parsed : SortOrder.New;
}
