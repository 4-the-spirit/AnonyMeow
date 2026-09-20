using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class PlatformAdminService(AppDbContext dbContext) : IPlatformAdminService
{
    public async Task<(IReadOnlyList<Report> Items, int TotalCount)> ListAllReportsAsync(
        ReportStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Reports.AsQueryable();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<SpamFlag> Items, int TotalCount)> ListSpamFlagsAsync(
        SpamFlagStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.SpamFlags.AsQueryable();
        if (status is not null)
        {
            query = query.Where(f => f.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<ModerationAction> Items, int TotalCount)> ListAuditLogAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.ModerationActions.CountAsync(cancellationToken);
        var items = await dbContext.ModerationActions
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
