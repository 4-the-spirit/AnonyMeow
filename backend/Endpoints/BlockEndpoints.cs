using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Dtos.Blocks;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

public static class BlockEndpoints
{
    public static IEndpointRouteBuilder MapBlockEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/users/{username}/block", BlockUserAsync).WithName("BlockUser");
        app.MapDelete("/api/users/{username}/block", UnblockUserAsync).WithName("UnblockUser");

        return app;
    }

    private static async Task<AppUser?> FindByUsernameAsync(AppDbContext dbContext, string username, CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        return await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized, cancellationToken);
    }

    private static async Task<Results<Created<BlockResponse>, NotFound>> BlockUserAsync(
        string username,
        IBlockService blockService,
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
        await blockService.BlockAsync(currentUser.Id, target.Id, cancellationToken);

        return TypedResults.Created(
            $"/api/users/{username}/block", new BlockResponse(username, DateTimeOffset.UtcNow));
    }

    private static async Task<Results<NoContent, NotFound>> UnblockUserAsync(
        string username,
        IBlockService blockService,
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
        await blockService.UnblockAsync(currentUser.Id, target.Id, cancellationToken);
        return TypedResults.NoContent();
    }
}
