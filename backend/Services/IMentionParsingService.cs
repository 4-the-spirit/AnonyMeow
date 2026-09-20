using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface IMentionParsingService
{
    Task<IReadOnlyList<AppUser>> ExtractMentionedUsersAsync(string bodyMarkdown, CancellationToken cancellationToken = default);
}
