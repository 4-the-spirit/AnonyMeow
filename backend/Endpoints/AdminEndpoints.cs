using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Admin;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

public static class AdminEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin").RequireAuthorization("PlatformAdmin");

        group.MapGet("/reports", ListAllReportsAsync).WithName("ListAllReports");
        group.MapGet("/spam-flags", ListSpamFlagsAsync).WithName("ListSpamFlags");
        group.MapPost("/users/{username}/restrict", RestrictUserAsync).WithName("RestrictUser");
        group.MapDelete("/users/{username}/restrict", LiftRestrictionAsync).WithName("LiftRestriction");
        group.MapGet("/audit-log", ListAuditLogAsync).WithName("ListAuditLog");

        return app;
    }

    private static async Task<Ok<PagedResponse<ReportResponse>>> ListAllReportsAsync(
        IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken,
        string? status = null,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var parsedStatus = Enum.TryParse<ReportStatus>(status, ignoreCase: true, out var s) ? s : (ReportStatus?)null;
        var (items, totalCount) = await platformAdminService.ListAllReportsAsync(parsedStatus, page, DefaultPageSize, cancellationToken);

        var responses = items.Select(ReportResponse.FromEntity).ToList();
        return TypedResults.Ok(new PagedResponse<ReportResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Ok<PagedResponse<SpamFlagResponse>>> ListSpamFlagsAsync(
        IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken,
        string? status = null,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var parsedStatus = Enum.TryParse<SpamFlagStatus>(status, ignoreCase: true, out var s) ? s : (SpamFlagStatus?)null;
        var (items, totalCount) = await platformAdminService.ListSpamFlagsAsync(parsedStatus, page, DefaultPageSize, cancellationToken);

        var responses = items.Select(SpamFlagResponse.FromEntity).ToList();
        return TypedResults.Ok(new PagedResponse<SpamFlagResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Results<Created<PlatformRestrictionResponse>, NotFound>> RestrictUserAsync(
        string username,
        CreatePlatformRestrictionRequest request,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var targetUser = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (targetUser is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var restriction = await moderationActionService.RestrictAsync(
            targetUser.Id, currentUser.Id, request.Type, request.Reason, request.EndAtUtc, cancellationToken);

        return TypedResults.Created(
            $"/api/admin/users/{username}/restrict",
            PlatformRestrictionResponse.FromEntity(restriction, targetUser.Username ?? string.Empty));
    }

    private static async Task<Results<NoContent, NotFound>> LiftRestrictionAsync(
        string username,
        IModerationActionService moderationActionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var targetUser = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (targetUser is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await moderationActionService.LiftRestrictionAsync(targetUser.Id, currentUser.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResponse<ModerationActionResponse>>> ListAuditLogAsync(
        IPlatformAdminService platformAdminService,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var (items, totalCount) = await platformAdminService.ListAuditLogAsync(page, DefaultPageSize, cancellationToken);

        var responses = items
            .Select(a => new ModerationActionResponse(a.Id, a.ActionType, a.TargetId, a.Reason, a.CreatedAtUtc))
            .ToList();
        return TypedResults.Ok(new PagedResponse<ModerationActionResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<AppUser?> FindByUsernameAsync(AppDbContext dbContext, string username, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        return await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
    }
}
