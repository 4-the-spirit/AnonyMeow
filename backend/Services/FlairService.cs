using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Dtos.Flairs;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class FlairService(AppDbContext dbContext) : IFlairService
{
    public async Task<Flair> CreateAsync(
        Guid communityId, string name, string colorHex, Guid modId, CancellationToken cancellationToken = default)
    {
        var nameExists = await dbContext.Flairs.AnyAsync(
            f => f.CommunityId == communityId && f.Name == name, cancellationToken);
        if (nameExists)
        {
            throw new FlairNameConflictException(name);
        }

        var flair = new Flair
        {
            Id = Guid.NewGuid(),
            CommunityId = communityId,
            Name = name,
            ColorHex = colorHex,
            CreatedByModId = modId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Flairs.Add(flair);
        await dbContext.SaveChangesAsync(cancellationToken);
        return flair;
    }

    public Task<Flair?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Flairs.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Flair>> ListForCommunityAsync(Guid communityId, CancellationToken cancellationToken = default) =>
        await dbContext.Flairs
            .Where(f => f.CommunityId == communityId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

    public async Task DeleteAsync(Flair flair, CancellationToken cancellationToken = default)
    {
        if (flair.IsDefault)
        {
            throw new FlairImmutableException();
        }

        dbContext.Flairs.Remove(flair);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Flair> UpdateAsync(
        Flair flair, string name, string colorHex, CancellationToken cancellationToken = default)
    {
        if (flair.IsDefault)
        {
            throw new FlairImmutableException();
        }

        var nameExists = await dbContext.Flairs.AnyAsync(
            f => f.CommunityId == flair.CommunityId && f.Name == name && f.Id != flair.Id, cancellationToken);
        if (nameExists)
        {
            throw new FlairNameConflictException(name);
        }

        flair.Name = name;
        flair.ColorHex = colorHex;
        await dbContext.SaveChangesAsync(cancellationToken);
        return flair;
    }

    public async Task AssignToPostAsync(Post post, Guid? flairId, CancellationToken cancellationToken = default)
    {
        if (flairId is not null)
        {
            await EnsureValidForCommunityAsync(flairId.Value, post.CommunityId, cancellationToken);
        }

        post.FlairId = flairId;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task EnsureValidForCommunityAsync(
        Guid flairId, Guid communityId, CancellationToken cancellationToken = default)
    {
        var flair = await dbContext.Flairs.FirstOrDefaultAsync(f => f.Id == flairId, cancellationToken)
            ?? throw new FlairNotFoundException();
        if (flair.CommunityId != communityId)
        {
            throw new FlairCommunityMismatchException();
        }
    }

    public async Task<FlairResponse?> GetResponseForPostAsync(Post post, CancellationToken cancellationToken = default)
    {
        if (post.FlairId is null)
        {
            return null;
        }

        var flair = await dbContext.Flairs.FirstOrDefaultAsync(f => f.Id == post.FlairId, cancellationToken);
        return flair is null ? null : FlairResponse.FromEntity(flair);
    }

    public async Task<IReadOnlyDictionary<Guid, FlairResponse>> GetResponseMapAsync(
        IEnumerable<Guid> flairIds, CancellationToken cancellationToken = default)
    {
        var ids = flairIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, FlairResponse>();
        }

        return await dbContext.Flairs
            .Where(f => ids.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => FlairResponse.FromEntity(f), cancellationToken);
    }
}
