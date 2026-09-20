using AnonyMeow.Domain;

namespace AnonyMeow.Dtos.Flairs;

public record FlairResponse(Guid Id, string Name, string ColorHex, bool IsDefault)
{
    public static FlairResponse FromEntity(Flair flair) => new(flair.Id, flair.Name, flair.ColorHex, flair.IsDefault);
}
