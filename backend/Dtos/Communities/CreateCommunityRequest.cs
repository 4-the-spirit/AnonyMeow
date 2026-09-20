using AnonyMeow.Dtos.Flairs;

namespace AnonyMeow.Dtos.Communities;

// Every community also gets DefaultFlairs.Names seeded automatically, so Flairs here is just an
// optional, additional custom-tag set. IconImageUrl/BannerImageUrl are required (validated in
// CommunityEndpoints.CreateCommunityAsync, not here — DTOs stay validation-free records, same
// convention as every other request DTO in this codebase).
public record CreateCommunityRequest(
    string Name,
    string? Description,
    IReadOnlyList<CommunityRuleRequest>? Rules,
    IReadOnlyList<CreateFlairRequest>? Flairs,
    string IconImageUrl,
    string BannerImageUrl);
