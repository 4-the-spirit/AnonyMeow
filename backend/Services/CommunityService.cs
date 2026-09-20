using AnonyMeow.Common;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Flairs;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class CommunityService(AppDbContext dbContext, IImageUploadService imageUploadService) : ICommunityService
{
    public async Task<Community> CreateAsync(
        string name,
        string? description,
        IReadOnlyList<CommunityRuleRequest>? rules,
        Guid creatorUserId,
        IReadOnlyList<CreateFlairRequest>? flairs,
        string iconImageUrl,
        string bannerImageUrl,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLowerInvariant();
        if (await dbContext.Communities.AnyAsync(c => c.Name.ToLower() == normalized, cancellationToken))
        {
            throw new CommunityNameConflictException(name);
        }

        // Icon/banner are mandatory (non-blank, enforced in CommunityEndpoints) but deliberately
        // NOT run through ImageUploadService.ValidateImageAsync here — that call depends on Azure
        // Blob Storage, which isn't provisioned yet anywhere in this codebase (see
        // BlobStorageNotConfiguredException / ImageUploadService.GetContainerClient). Requiring a
        // real validated blob at creation would make community creation entirely non-functional
        // until that infrastructure exists. UpdateAsync below still validates when an image is
        // provided post-creation, matching the rest of the app's existing image-upload paths.

        var community = new Community
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IconImageUrl = iconImageUrl,
            BannerImageUrl = bannerImageUrl,
            CreatedByUserId = creatorUserId,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Communities.Add(community);
        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id,
            AppUserId = creatorUserId,
            Role = CommunityRole.Moderator,
            JoinedAtUtc = DateTimeOffset.UtcNow
        });

        // Added to the same SaveChangesAsync call as the community/membership above so all three
        // commit in one transaction — a mid-way failure can't leave a tagless community.
        foreach (var defaultFlairName in DefaultFlairs.Names)
        {
            dbContext.Flairs.Add(new Flair
            {
                Id = Guid.NewGuid(),
                CommunityId = community.Id,
                Name = defaultFlairName,
                ColorHex = DefaultFlairs.NameToColorHex[defaultFlairName],
                IsDefault = true,
                CreatedByModId = creatorUserId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        if (flairs is { Count: > 0 })
        {
            foreach (var flair in flairs)
            {
                dbContext.Flairs.Add(new Flair
                {
                    Id = Guid.NewGuid(),
                    CommunityId = community.Id,
                    Name = flair.Name,
                    ColorHex = flair.ColorHex,
                    CreatedByModId = creatorUserId,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        if (rules is { Count: > 0 })
        {
            for (var i = 0; i < rules.Count; i++)
            {
                dbContext.CommunityRules.Add(new CommunityRule
                {
                    Id = Guid.NewGuid(),
                    CommunityId = community.Id,
                    Title = rules[i].Title,
                    Description = rules[i].Description,
                    Order = i,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return community;
    }

    public async Task<IReadOnlyList<CommunityRule>> ListRulesAsync(
        Guid communityId, CancellationToken cancellationToken = default) =>
        await dbContext.CommunityRules
            .Where(r => r.CommunityId == communityId)
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);

    public Task<Community?> GetEntityByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name.ToLowerInvariant();
        return dbContext.Communities.SingleOrDefaultAsync(c => c.Name.ToLower() == normalized, cancellationToken);
    }

    public Task<int> GetMemberCountAsync(Guid communityId, CancellationToken cancellationToken = default) =>
        dbContext.CommunityMemberships.CountAsync(m => m.CommunityId == communityId, cancellationToken);

    public async Task<(IReadOnlyList<Community> Items, int TotalCount)> SearchAsync(
        string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Communities.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            // Bidirectional on Name (a short field): a search also matches when the search text
            // itself contains the (shorter) community name, not just the reverse. Description is
            // long free text, so it stays forward-only (target-contains-query).
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                EF.Functions.ILike(search, "%" + c.Name + "%") ||
                (c.Description != null && EF.Functions.ILike(c.Description, pattern)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(
        Community community,
        Guid editorUserId,
        string? description,
        IReadOnlyList<CommunityRuleRequest>? rules,
        string? iconImageUrl = null,
        string? bannerImageUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (description is not null)
        {
            community.Description = description;
        }

        // Replaces the entire rule set (not an append/merge) — simplest correct semantics for an
        // edit form that always submits its full current list.
        if (rules is not null)
        {
            var existingRules = await dbContext.CommunityRules
                .Where(r => r.CommunityId == community.Id)
                .ToListAsync(cancellationToken);
            dbContext.CommunityRules.RemoveRange(existingRules);

            for (var i = 0; i < rules.Count; i++)
            {
                dbContext.CommunityRules.Add(new CommunityRule
                {
                    Id = Guid.NewGuid(),
                    CommunityId = community.Id,
                    Title = rules[i].Title,
                    Description = rules[i].Description,
                    Order = i,
                    CreatedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        if (iconImageUrl is not null)
        {
            await imageUploadService.ValidateImageAsync(iconImageUrl, editorUserId, cancellationToken);
            community.IconImageUrl = iconImageUrl;
        }

        if (bannerImageUrl is not null)
        {
            await imageUploadService.ValidateImageAsync(bannerImageUrl, editorUserId, cancellationToken);
            community.BannerImageUrl = bannerImageUrl;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task JoinAsync(Community community, Guid userId, CancellationToken cancellationToken = default)
    {
        var alreadyMember = await dbContext.CommunityMemberships
            .AnyAsync(m => m.CommunityId == community.Id && m.AppUserId == userId, cancellationToken);
        if (alreadyMember)
        {
            return;
        }

        dbContext.CommunityMemberships.Add(new CommunityMembership
        {
            CommunityId = community.Id,
            AppUserId = userId,
            Role = CommunityRole.Member,
            JoinedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task LeaveAsync(Community community, Guid userId, CancellationToken cancellationToken = default)
    {
        var membership = await dbContext.CommunityMemberships
            .SingleOrDefaultAsync(m => m.CommunityId == community.Id && m.AppUserId == userId, cancellationToken);
        if (membership is null)
        {
            return;
        }

        if (membership.Role == CommunityRole.Moderator)
        {
            var moderatorCount = await dbContext.CommunityMemberships.CountAsync(
                m => m.CommunityId == community.Id && m.Role == CommunityRole.Moderator, cancellationToken);
            if (moderatorCount <= 1)
            {
                throw new SoleModeratorLeaveException(community.Name);
            }
        }

        dbContext.CommunityMemberships.Remove(membership);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommunityModeratorResponse>> ListModeratorsAsync(
        Guid communityId, CancellationToken cancellationToken = default) =>
        await dbContext.CommunityMemberships
            .Where(m => m.CommunityId == communityId && m.Role == CommunityRole.Moderator)
            .Join(dbContext.Users, m => m.AppUserId, u => u.Id,
                (m, u) => new CommunityModeratorResponse(u.Username ?? string.Empty, u.DisplayName, m.JoinedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<CommunityMembershipResponse> Items, int TotalCount)> ListJoinedByUsernameAsync(
        string username, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalized = username.ToLowerInvariant();
        var query = dbContext.CommunityMemberships
            .Join(dbContext.Users, m => m.AppUserId, u => u.Id, (m, u) => new { m, u })
            .Where(x => x.u.Username != null && x.u.Username.ToLower() == normalized)
            .Join(dbContext.Communities, x => x.m.CommunityId, c => c.Id, (x, c) => new { x.m, c });

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.m.JoinedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CommunityMembershipResponse(x.c.Name, x.c.IconImageUrl, x.m.Role, x.m.JoinedAtUtc))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<CommunityMemberResponse> Items, int TotalCount)> ListMembersAsync(
        Guid communityId, string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.CommunityMemberships
            .Where(m => m.CommunityId == communityId)
            .Join(dbContext.Users, m => m.AppUserId, u => u.Id, (m, u) => new { m, u });

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            // Bidirectional: username/display name are short fields, so also match when the
            // search text itself contains the (shorter) name.
            query = query.Where(x =>
                (x.u.Username != null && (EF.Functions.ILike(x.u.Username, pattern) || EF.Functions.ILike(search, "%" + x.u.Username + "%"))) ||
                (x.u.DisplayName != null && (EF.Functions.ILike(x.u.DisplayName, pattern) || EF.Functions.ILike(search, "%" + x.u.DisplayName + "%"))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.m.Role)
            .ThenBy(x => x.u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CommunityMemberResponse(x.u.Username ?? string.Empty, x.u.DisplayName, x.u.AvatarSeed, x.m.Role, x.m.JoinedAtUtc))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
