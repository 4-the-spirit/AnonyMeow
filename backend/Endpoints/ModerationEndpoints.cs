using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

public static class ModerationEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/posts/{id:guid}/reports", CreatePostReportAsync).WithName("CreatePostReport").RequireRateLimiting("Report");
        app.MapPost("/api/comments/{id:guid}/reports", CreateCommentReportAsync).WithName("CreateCommentReport").RequireRateLimiting("Report");

        app.MapGet("/api/communities/{name}/mod/reports", ListReportsAsync).WithName("ListCommunityReports");
        app.MapPost("/api/communities/{name}/mod/reports/{id:guid}/resolve", ResolveReportAsync).WithName("ResolveReport");

        app.MapPost("/api/posts/{id:guid}/mod/pin", PinPostAsync).WithName("PinPost");
        app.MapDelete("/api/posts/{id:guid}/mod/pin", UnpinPostAsync).WithName("UnpinPost");
        app.MapPost("/api/posts/{id:guid}/mod/lock", LockPostAsync).WithName("LockPost");
        app.MapDelete("/api/posts/{id:guid}/mod/lock", UnlockPostAsync).WithName("UnlockPost");
        app.MapPost("/api/posts/{id:guid}/mod/remove", RemovePostAsync).WithName("RemovePostByMod");
        app.MapPost("/api/comments/{id:guid}/mod/remove", RemoveCommentAsync).WithName("RemoveCommentByMod");

        app.MapPost("/api/communities/{name}/mod/bans", CreateBanAsync).WithName("CreateBan");
        app.MapGet("/api/communities/{name}/mod/bans", ListBansAsync).WithName("ListBans");
        app.MapDelete("/api/communities/{name}/mod/bans/{username}", DeleteBanAsync).WithName("DeleteBan");

        app.MapPost("/api/communities/{name}/mod/moderators/{username}", PromoteModeratorAsync).WithName("PromoteModerator");
        app.MapDelete("/api/communities/{name}/mod/moderators/{username}", DemoteModeratorAsync).WithName("DemoteModerator");

        return app;
    }

    private static async Task<Results<Created<ReportResponse>, NotFound>> CreatePostReportAsync(
        Guid id,
        CreateReportRequest request,
        IPostService postService,
        IReportService reportService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        var report = await reportService.CreateAsync(
            ReportTargetType.Post, id, currentUser.Id, request.Category, request.Details, cancellationToken);
        return TypedResults.Created($"/api/posts/{id}/reports/{report.Id}", ReportResponse.FromEntity(report));
    }

    private static async Task<Results<Created<ReportResponse>, NotFound>> CreateCommentReportAsync(
        Guid id,
        CreateReportRequest request,
        ICommentService commentService,
        IReportService reportService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var report = await reportService.CreateAsync(
            ReportTargetType.Comment, id, currentUser.Id, request.Category, request.Details, cancellationToken);
        return TypedResults.Created($"/api/comments/{id}/reports/{report.Id}", ReportResponse.FromEntity(report));
    }

    private static async Task<Results<Ok<PagedResponse<ReportResponse>>, NotFound, ForbidHttpResult>> ListReportsAsync(
        string name,
        ICommunityService communityService,
        IReportService reportService,
        IAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        string? status = null,
        int page = 1)
    {
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

        var parsedStatus = Enum.TryParse<ReportStatus>(status, ignoreCase: true, out var s) ? s : (ReportStatus?)null;
        page = Math.Max(page, 1);
        var (items, totalCount) = await reportService.ListForCommunityAsync(
            community.Id, parsedStatus, page, DefaultPageSize, cancellationToken);

        var responses = items.Select(ReportResponse.FromEntity).ToList();
        return TypedResults.Ok(new PagedResponse<ReportResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Results<Ok<ReportResponse>, NotFound, ForbidHttpResult>> ResolveReportAsync(
        string name,
        Guid id,
        ResolveReportRequest request,
        ICommunityService communityService,
        IReportService reportService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
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

        var report = await reportService.GetByIdAsync(id, cancellationToken);
        if (report is null)
        {
            return TypedResults.NotFound();
        }

        // The caller is only ever authorized as a moderator of `community` above — without this
        // check, any moderator of any community could resolve a report belonging to a different
        // community by ID (report IDs are visible to their own reporter), corrupting that other
        // community's mod queue and audit trail. 404 rather than 403 to match the rest of this
        // file's pattern of not confirming a report's existence to a non-authorized caller.
        var reportCommunityId = await reportService.GetCommunityIdForReportAsync(report, cancellationToken);
        if (reportCommunityId != community.Id)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await reportService.ResolveAsync(
            report, community.Id, currentUser.Id, request.Outcome, request.ActionReason, cancellationToken);

        return TypedResults.Ok(ReportResponse.FromEntity(report));
    }

    private static Task<Results<NoContent, NotFound, ForbidHttpResult>> PinPostAsync(
        Guid id, IPostService postService, IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor, IAuthorizationService authorizationService,
        AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
        ApplyPostModActionAsync(id, postService, moderationActionService, currentUserAccessor, authorizationService,
            dbContext, httpContext, cancellationToken, (svc, post, modId, ct) => svc.PinAsync(post, modId, null, ct));

    private static Task<Results<NoContent, NotFound, ForbidHttpResult>> UnpinPostAsync(
        Guid id, IPostService postService, IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor, IAuthorizationService authorizationService,
        AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
        ApplyPostModActionAsync(id, postService, moderationActionService, currentUserAccessor, authorizationService,
            dbContext, httpContext, cancellationToken, (svc, post, modId, ct) => svc.UnpinAsync(post, modId, null, ct));

    private static Task<Results<NoContent, NotFound, ForbidHttpResult>> LockPostAsync(
        Guid id, IPostService postService, IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor, IAuthorizationService authorizationService,
        AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
        ApplyPostModActionAsync(id, postService, moderationActionService, currentUserAccessor, authorizationService,
            dbContext, httpContext, cancellationToken, (svc, post, modId, ct) => svc.LockAsync(post, modId, null, ct));

    private static Task<Results<NoContent, NotFound, ForbidHttpResult>> UnlockPostAsync(
        Guid id, IPostService postService, IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor, IAuthorizationService authorizationService,
        AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
        ApplyPostModActionAsync(id, postService, moderationActionService, currentUserAccessor, authorizationService,
            dbContext, httpContext, cancellationToken, (svc, post, modId, ct) => svc.UnlockAsync(post, modId, null, ct));

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> RemovePostAsync(
        Guid id, string? reason, IPostService postService, IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor, IAuthorizationService authorizationService,
        AppDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
        await ApplyPostModActionAsync(id, postService, moderationActionService, currentUserAccessor, authorizationService,
            dbContext, httpContext, cancellationToken, (svc, post, modId, ct) => svc.RemoveAsync(post, modId, reason, ct));

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> ApplyPostModActionAsync(
        Guid id,
        IPostService postService,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        Func<IModerationActionService, Post, Guid, CancellationToken, Task> action)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        var community = await dbContext.Communities.FindAsync([post.CommunityId], cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(httpContext.User, community, "CommunityModerator");
        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        await action(moderationActionService, post, currentUser.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> RemoveCommentAsync(
        Guid id,
        string? reason,
        ICommentService commentService,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        // Comments don't store CommunityId directly, so the community has to be resolved via the
        // comment's post, same as ApplyPostModActionAsync resolves it via the post's CommunityId.
        var post = await dbContext.Posts.FindAsync([comment.PostId], cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        var community = await dbContext.Communities.FindAsync([post.CommunityId], cancellationToken);
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
        await moderationActionService.RemoveCommentAsync(comment, community.Id, currentUser.Id, reason, cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<BanResponse>, NotFound, ForbidHttpResult>> CreateBanAsync(
        string name,
        CreateBanRequest request,
        ICommunityService communityService,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
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

        var normalized = request.Username.ToLowerInvariant();
        var targetUser = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
        if (targetUser is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await moderationActionService.BanAsync(community, targetUser.Id, currentUser.Id, request.Reason, cancellationToken);

        return TypedResults.Created(
            $"/api/communities/{Uri.EscapeDataString(name)}/mod/bans/{Uri.EscapeDataString(targetUser.Username ?? string.Empty)}",
            new BanResponse(targetUser.Username ?? string.Empty, request.Reason, DateTimeOffset.UtcNow));
    }

    private static async Task<Results<Ok<IReadOnlyList<BanResponse>>, NotFound, ForbidHttpResult>> ListBansAsync(
        string name,
        ICommunityService communityService,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
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

        var bans = await dbContext.CommunityBans
            .Where(b => b.CommunityId == community.Id)
            .Join(dbContext.Users, b => b.AppUserId, u => u.Id,
                (b, u) => new BanResponse(u.Username ?? string.Empty, b.Reason, b.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<BanResponse>>(bans);
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteBanAsync(
        string name,
        string username,
        ICommunityService communityService,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
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

        var normalized = username.ToLowerInvariant();
        var targetUser = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
        if (targetUser is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await moderationActionService.UnbanAsync(community, targetUser.Id, currentUser.Id, cancellationToken);

        return TypedResults.NoContent();
    }

    // Throws CommunityMemberNotFoundException (404, via ApiException) if the target isn't a member.
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> PromoteModeratorAsync(
        string name,
        string username,
        ICommunityService communityService,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
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

        var normalized = username.ToLowerInvariant();
        var targetUser = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
        if (targetUser is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await moderationActionService.PromoteToModeratorAsync(community, targetUser.Id, currentUser.Id, cancellationToken);

        return TypedResults.NoContent();
    }

    // Throws CommunityMemberNotFoundException (404) if the target isn't currently a moderator, or
    // SoleModeratorDemoteException (409, via ApiException) if they're the community's last moderator.
    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DemoteModeratorAsync(
        string name,
        string username,
        ICommunityService communityService,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        AppDbContext dbContext,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
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

        var normalized = username.ToLowerInvariant();
        var targetUser = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
        if (targetUser is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await moderationActionService.DemoteModeratorAsync(community, targetUser.Id, currentUser.Id, cancellationToken);

        return TypedResults.NoContent();
    }
}
