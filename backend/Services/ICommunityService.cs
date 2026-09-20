using AnonyMeow.Domain;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;

namespace AnonyMeow.Services;

public interface ICommunityService
{
    // `flairs` is the optional, additional custom-tag set on top of the always-seeded
    // DefaultFlairs.Names — enforcement of any of this DTO shape happens in CommunityEndpoints,
    // same as every other request-shape validation in this codebase (services trust
    // already-validated input). Icon/banner are mandatory at creation (no default), unlike
    // UpdateAsync below where they stay optional.
    Task<Community> CreateAsync(
        string name,
        string? description,
        IReadOnlyList<CommunityRuleRequest>? rules,
        Guid creatorUserId,
        IReadOnlyList<CreateFlairRequest>? flairs,
        string iconImageUrl,
        string bannerImageUrl,
        CancellationToken cancellationToken = default);

    Task<Community?> GetEntityByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<int> GetMemberCountAsync(Guid communityId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Community> Items, int TotalCount)> SearchAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommunityRule>> ListRulesAsync(Guid communityId, CancellationToken cancellationToken = default);

    // `rules`, when non-null, replaces the community's entire rule list.
    Task UpdateAsync(
        Community community,
        Guid editorUserId,
        string? description,
        IReadOnlyList<CommunityRuleRequest>? rules,
        string? iconImageUrl = null,
        string? bannerImageUrl = null,
        CancellationToken cancellationToken = default);

    Task JoinAsync(Community community, Guid userId, CancellationToken cancellationToken = default);

    // Throws SoleModeratorLeaveException if the user is the community's last moderator.
    Task LeaveAsync(Community community, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommunityModeratorResponse>> ListModeratorsAsync(
        Guid communityId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CommunityMembershipResponse> Items, int TotalCount)> ListJoinedByUsernameAsync(
        string username, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CommunityMemberResponse> Items, int TotalCount)> ListMembersAsync(
        Guid communityId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
}
