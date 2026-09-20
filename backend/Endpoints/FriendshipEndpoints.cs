using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Friends;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

public static class FriendshipEndpoints
{
    public static IEndpointRouteBuilder MapFriendshipEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/{username}/friend-requests", SendFriendRequestAsync).WithName("SendFriendRequest");
        app.MapDelete("/api/users/{username}/friend-requests", CancelFriendRequestAsync).WithName("CancelFriendRequest");
        app.MapPost("/api/users/{username}/friend-requests/accept", AcceptFriendRequestAsync).WithName("AcceptFriendRequest");
        app.MapPost("/api/users/{username}/friend-requests/decline", DeclineFriendRequestAsync).WithName("DeclineFriendRequest");
        app.MapDelete("/api/friends/{username}", RemoveFriendAsync).WithName("RemoveFriend");
        app.MapGet("/api/users/{username}/friends", ListFriendsAsync).WithName("ListFriends");
        app.MapGet("/api/users/me/friend-requests", ListMyFriendRequestsAsync).WithName("ListMyFriendRequests");

        return app;
    }

    private static async Task<AppUser?> FindByUsernameAsync(AppDbContext dbContext, string username, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        return await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
    }

    private static async Task<Results<Created<FriendRequestResponse>, NotFound>> SendFriendRequestAsync(
        string username,
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var addressee = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (addressee is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var friendship = await friendshipService.RequestAsync(currentUser.Id, addressee.Id, cancellationToken);

        return TypedResults.Created(
            $"/api/users/{username}/friend-requests",
            FriendRequestResponse.FromEntity(
                friendship, currentUser.Username ?? string.Empty, addressee.Username ?? string.Empty,
                currentUser.DisplayName, addressee.DisplayName));
    }

    private static async Task<Results<NoContent, NotFound>> CancelFriendRequestAsync(
        string username,
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var addressee = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (addressee is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await friendshipService.CancelRequestAsync(currentUser.Id, addressee.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<FriendRequestResponse>, NotFound>> AcceptFriendRequestAsync(
        string username,
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var requester = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (requester is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var friendship = await friendshipService.GetPendingRequestAsync(requester.Id, currentUser.Id, cancellationToken);
        if (friendship is null)
        {
            return TypedResults.NotFound();
        }

        await friendshipService.AcceptAsync(friendship, cancellationToken);

        return TypedResults.Ok(FriendRequestResponse.FromEntity(
            friendship, requester.Username ?? string.Empty, currentUser.Username ?? string.Empty,
            requester.DisplayName, currentUser.DisplayName));
    }

    private static async Task<Results<Ok<FriendRequestResponse>, NotFound>> DeclineFriendRequestAsync(
        string username,
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var requester = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (requester is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var friendship = await friendshipService.GetPendingRequestAsync(requester.Id, currentUser.Id, cancellationToken);
        if (friendship is null)
        {
            return TypedResults.NotFound();
        }

        await friendshipService.DeclineAsync(friendship, cancellationToken);

        return TypedResults.Ok(FriendRequestResponse.FromEntity(
            friendship, requester.Username ?? string.Empty, currentUser.Username ?? string.Empty,
            requester.DisplayName, currentUser.DisplayName));
    }

    private static async Task<Results<NoContent, NotFound>> RemoveFriendAsync(
        string username,
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var other = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (other is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await friendshipService.RemoveAsync(currentUser.Id, other.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IReadOnlyList<FriendResponse>>, NotFound, ForbidHttpResult>> ListFriendsAsync(
        string username,
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var target = await FindByUsernameAsync(dbContext, username, cancellationToken);
        if (target is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (currentUser.Id != target.Id)
        {
            var visible = target.FriendListVisibility switch
            {
                FriendListVisibility.Everyone => true,
                FriendListVisibility.NoOne => false,
                FriendListVisibility.FriendsOnly =>
                    await friendshipService.AreFriendsAsync(currentUser.Id, target.Id, cancellationToken),
                _ => false
            };

            if (!visible)
            {
                return TypedResults.Forbid();
            }
        }

        var friends = await friendshipService.ListFriendsAsync(target.Id, cancellationToken);
        var responses = friends
            .Select(f => new FriendResponse(f.Friend.Username ?? string.Empty, f.Friend.DisplayName, f.Friend.AvatarSeed, f.FriendsSinceUtc))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<FriendResponse>>(responses);
    }

    private static async Task<Ok<FriendRequestsResponse>> ListMyFriendRequestsAsync(
        IFriendshipService friendshipService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var (incoming, outgoing) = await friendshipService.ListPendingAsync(currentUser.Id, cancellationToken);

        var currentUsername = currentUser.Username ?? string.Empty;
        var incomingResponses = incoming
            .Select(p => new FriendRequestResponse(
                p.OtherUser.Username ?? string.Empty, p.OtherUser.DisplayName,
                currentUsername, currentUser.DisplayName, FriendshipStatus.Pending, p.CreatedAtUtc))
            .ToList();
        var outgoingResponses = outgoing
            .Select(p => new FriendRequestResponse(
                currentUsername, currentUser.DisplayName,
                p.OtherUser.Username ?? string.Empty, p.OtherUser.DisplayName, FriendshipStatus.Pending, p.CreatedAtUtc))
            .ToList();

        return TypedResults.Ok(new FriendRequestsResponse(incomingResponses, outgoingResponses));
    }
}
