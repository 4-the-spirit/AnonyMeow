using System.Text.RegularExpressions;
using AnonyMeow.Common;
using AnonyMeow.Common.Middleware;
using AnonyMeow.Domain;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AnonyMeow.Endpoints;

public static partial class CommunityEndpoints
{
    private const int DefaultPageSize = 20;

    // Matches the zod schemas already enforced client-side (CreateCommunityPage/CommunitySettingsPage)
    // — the backend just didn't enforce the same bound.
    private const int MaxDescriptionLength = 500;
    private const int MaxRuleTitleLength = 100;
    private const int MaxRuleDescriptionLength = 500;

    [GeneratedRegex("^[א-ת0-9_ -]{3,30}$")]
    private static partial Regex NamePattern();

    private static IDictionary<string, string[]> ValidateTextLengths(string? description)
    {
        var errors = new Dictionary<string, string[]>();
        if (description is { Length: > MaxDescriptionLength })
        {
            errors["description"] = [$"Description cannot exceed {MaxDescriptionLength} characters."];
        }

        return errors;
    }

    // Rules are optional — a community can launch/stay with zero rules — but whatever rules are
    // submitted must each have a non-blank title/description within bounds.
    private static IDictionary<string, string[]> ValidateRules(IReadOnlyList<CommunityRuleRequest>? rules)
    {
        var errors = new Dictionary<string, string[]>();
        if (rules is not { Count: > 0 })
        {
            return errors;
        }

        for (var i = 0; i < rules.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(rules[i].Title))
            {
                errors[$"rules[{i}].title"] = ["Rule title is required."];
            }
            else if (rules[i].Title.Length > MaxRuleTitleLength)
            {
                errors[$"rules[{i}].title"] = [$"Rule title cannot exceed {MaxRuleTitleLength} characters."];
            }

            if (string.IsNullOrWhiteSpace(rules[i].Description))
            {
                errors[$"rules[{i}].description"] = ["Rule description is required."];
            }
            else if (rules[i].Description.Length > MaxRuleDescriptionLength)
            {
                errors[$"rules[{i}].description"] = [$"Rule description cannot exceed {MaxRuleDescriptionLength} characters."];
            }
        }

        return errors;
    }

    // Every community must have a banner and an icon at creation time — except in Development,
    // where blob storage isn't provisioned (ImageUploadField can't produce a real URL, see its
    // own comment), so CreateCommunityAsync fills in a placeholder instead of enforcing this.
    private static IDictionary<string, string[]> ValidateImages(string iconImageUrl, string bannerImageUrl, IHostEnvironment env)
    {
        var errors = new Dictionary<string, string[]>();
        if (env.IsDevelopment())
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(iconImageUrl))
        {
            errors["iconImageUrl"] = ["An icon image is required."];
        }

        if (string.IsNullOrWhiteSpace(bannerImageUrl))
        {
            errors["bannerImageUrl"] = ["A banner image is required."];
        }

        return errors;
    }

    private const string DevPlaceholderIconUrl = "https://placehold.co/200x200/png?text=Icon";
    private const string DevPlaceholderBannerUrl = "https://placehold.co/800x200/png?text=Banner";

    // Every community launches with the fixed DefaultFlairs.Names set (seeded in
    // CommunityService.CreateAsync), so custom flairs here are optional — this only validates
    // whatever custom tags the creator chose to add, mirroring the per-field checks
    // FlairEndpoints.CreateFlairAsync already runs (name/color required), plus the
    // no-duplicate-names rule that only applies to this bulk-create path.
    private static IDictionary<string, string[]> ValidateFlairs(IReadOnlyList<CreateFlairRequest>? flairs)
    {
        var errors = new Dictionary<string, string[]>();
        if (flairs is not { Count: > 0 })
        {
            return errors;
        }

        for (var i = 0; i < flairs.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(flairs[i].Name))
            {
                errors[$"flairs[{i}].name"] = ["Tag name is required."];
            }

            if (string.IsNullOrWhiteSpace(flairs[i].ColorHex))
            {
                errors[$"flairs[{i}].colorHex"] = ["Tag color is required."];
            }

            if (DefaultFlairs.Names.Contains(flairs[i].Name))
            {
                errors[$"flairs[{i}].name"] = ["This tag is already included by default."];
            }
        }

        var duplicateNames = flairs
            .Select(f => f.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .GroupBy(n => n)
            .Any(g => g.Count() > 1);
        if (duplicateNames)
        {
            errors["flairs"] = ["Tag names must be unique."];
        }

        return errors;
    }

    public static IEndpointRouteBuilder MapCommunityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/communities");

        group.MapPost("/", CreateCommunityAsync).WithName("CreateCommunity");
        group.MapGet("/", SearchCommunitiesAsync).WithName("SearchCommunities").AllowAnonymous();
        group.MapGet("/{name}", GetCommunityAsync).WithName("GetCommunity").AllowAnonymous();
        group.MapPatch("/{name}", UpdateCommunityAsync).WithName("UpdateCommunity");
        group.MapPost("/{name}/join", JoinCommunityAsync).WithName("JoinCommunity");
        group.MapDelete("/{name}/leave", LeaveCommunityAsync).WithName("LeaveCommunity");
        group.MapGet("/{name}/moderators", GetModeratorsAsync).WithName("GetCommunityModerators").AllowAnonymous();
        group.MapGet("/{name}/members", GetMembersAsync).WithName("GetCommunityMembers").AllowAnonymous();

        return app;
    }

    private static async Task<Results<Created<CommunityResponse>, JsonHttpResult<HttpValidationProblemDetails>>> CreateCommunityAsync(
        CreateCommunityRequest request,
        ICurrentUserAccessor currentUserAccessor,
        ICommunityService communityService,
        IHostEnvironment env,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || !NamePattern().IsMatch(request.Name))
        {
            return ValidationProblemFactory.Create(
                "name", "Community name must be 3-30 characters: Hebrew letters, numbers, spaces, underscores, and hyphens only.");
        }

        var lengthErrors = ValidateTextLengths(request.Description);
        if (lengthErrors.Count > 0)
        {
            return ValidationProblemFactory.Create(lengthErrors);
        }

        var flairErrors = ValidateFlairs(request.Flairs);
        if (flairErrors.Count > 0)
        {
            return ValidationProblemFactory.Create(flairErrors);
        }

        var ruleErrors = ValidateRules(request.Rules);
        if (ruleErrors.Count > 0)
        {
            return ValidationProblemFactory.Create(ruleErrors);
        }

        var imageErrors = ValidateImages(request.IconImageUrl, request.BannerImageUrl, env);
        if (imageErrors.Count > 0)
        {
            return ValidationProblemFactory.Create(imageErrors);
        }

        var iconImageUrl = string.IsNullOrWhiteSpace(request.IconImageUrl) ? DevPlaceholderIconUrl : request.IconImageUrl;
        var bannerImageUrl = string.IsNullOrWhiteSpace(request.BannerImageUrl) ? DevPlaceholderBannerUrl : request.BannerImageUrl;

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var community = await communityService.CreateAsync(
            request.Name,
            request.Description,
            request.Rules,
            currentUser.Id,
            flairs: request.Flairs,
            iconImageUrl: iconImageUrl,
            bannerImageUrl: bannerImageUrl,
            cancellationToken: cancellationToken);

        var rules = await communityService.ListRulesAsync(community.Id, cancellationToken);
        return TypedResults.Created(
            $"/api/communities/{Uri.EscapeDataString(community.Name)}",
            CommunityResponse.FromEntity(community, memberCount: 1, rules));
    }

    private static async Task<Ok<PagedResponse<CommunityResponse>>> SearchCommunitiesAsync(
        ICommunityService communityService,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var (items, totalCount) = await communityService.SearchAsync(search, page, DefaultPageSize, cancellationToken);

        var responses = new List<CommunityResponse>(items.Count);
        foreach (var community in items)
        {
            var memberCount = await communityService.GetMemberCountAsync(community.Id, cancellationToken);
            responses.Add(CommunityResponse.FromEntity(community, memberCount));
        }

        return TypedResults.Ok(new PagedResponse<CommunityResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Results<Ok<CommunityResponse>, NotFound>> GetCommunityAsync(
        string name,
        ICommunityService communityService,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var memberCount = await communityService.GetMemberCountAsync(community.Id, cancellationToken);
        var rules = await communityService.ListRulesAsync(community.Id, cancellationToken);
        return TypedResults.Ok(CommunityResponse.FromEntity(community, memberCount, rules));
    }

    private static async Task<Results<Ok<CommunityResponse>, NotFound, ForbidHttpResult, JsonHttpResult<HttpValidationProblemDetails>>> UpdateCommunityAsync(
        string name,
        UpdateCommunityRequest request,
        HttpContext httpContext,
        ICommunityService communityService,
        IAuthorizationService authorizationService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var lengthErrors = ValidateTextLengths(request.Description);
        if (lengthErrors.Count > 0)
        {
            return ValidationProblemFactory.Create(lengthErrors);
        }

        var ruleErrors = ValidateRules(request.Rules);
        if (ruleErrors.Count > 0)
        {
            return ValidationProblemFactory.Create(ruleErrors);
        }

        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(httpContext.User, community, "CommunityModerator");
        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await communityService.UpdateAsync(
            community, currentUser.Id, request.Description, request.Rules, request.IconImageUrl, request.BannerImageUrl,
            cancellationToken);

        var memberCount = await communityService.GetMemberCountAsync(community.Id, cancellationToken);
        var rules = await communityService.ListRulesAsync(community.Id, cancellationToken);
        return TypedResults.Ok(CommunityResponse.FromEntity(community, memberCount, rules));
    }

    private static async Task<Results<Ok<CommunityResponse>, NotFound>> JoinCommunityAsync(
        string name,
        ICurrentUserAccessor currentUserAccessor,
        ICommunityService communityService,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await communityService.JoinAsync(community, currentUser.Id, cancellationToken);

        var memberCount = await communityService.GetMemberCountAsync(community.Id, cancellationToken);
        var rules = await communityService.ListRulesAsync(community.Id, cancellationToken);
        return TypedResults.Ok(CommunityResponse.FromEntity(community, memberCount, rules));
    }

    private static async Task<Results<NoContent, NotFound>> LeaveCommunityAsync(
        string name,
        ICurrentUserAccessor currentUserAccessor,
        ICommunityService communityService,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await communityService.LeaveAsync(community, currentUser.Id, cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IReadOnlyList<CommunityModeratorResponse>>, NotFound>> GetModeratorsAsync(
        string name,
        ICommunityService communityService,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var moderators = await communityService.ListModeratorsAsync(community.Id, cancellationToken);
        return TypedResults.Ok(moderators);
    }

    private static async Task<Results<Ok<PagedResponse<CommunityMemberResponse>>, NotFound>> GetMembersAsync(
        string name,
        ICommunityService communityService,
        CancellationToken cancellationToken,
        string? search = null,
        int page = 1)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        page = Math.Max(page, 1);
        var (items, totalCount) = await communityService.ListMembersAsync(
            community.Id, search, page, DefaultPageSize, cancellationToken);

        return TypedResults.Ok(new PagedResponse<CommunityMemberResponse>(items, page, DefaultPageSize, totalCount));
    }
}
