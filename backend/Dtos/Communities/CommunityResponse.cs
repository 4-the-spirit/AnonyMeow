using AnonyMeow.Domain;

namespace AnonyMeow.Dtos.Communities;

public record CommunityResponse(
    string Name,
    string? Description,
    IReadOnlyList<CommunityRuleResponse> Rules,
    string IconImageUrl,
    string BannerImageUrl,
    int MemberCount,
    DateTimeOffset CreatedAtUtc)
{
    public static CommunityResponse FromEntity(
        Community community, int memberCount, IReadOnlyList<CommunityRule>? rules = null) => new(
        community.Name,
        community.Description,
        (rules ?? []).Select(CommunityRuleResponse.FromEntity).ToList(),
        community.IconImageUrl,
        community.BannerImageUrl,
        memberCount,
        community.CreatedAtUtc);
}
