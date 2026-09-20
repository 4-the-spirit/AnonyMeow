using AnonyMeow.Domain;

namespace AnonyMeow.Dtos.Communities;

public record CommunityRuleResponse(string Title, string Description)
{
    public static CommunityRuleResponse FromEntity(CommunityRule rule) => new(rule.Title, rule.Description);
}
