namespace AnonyMeow.Dtos.Communities;

// Rules, when provided, replaces the community's entire rule list (not an append/merge) — see
// CommunityService.UpdateAsync. Images stay optional here: editing shouldn't force a re-upload.
public record UpdateCommunityRequest(
    string? Description,
    IReadOnlyList<CommunityRuleRequest>? Rules,
    string? IconImageUrl = null,
    string? BannerImageUrl = null);
