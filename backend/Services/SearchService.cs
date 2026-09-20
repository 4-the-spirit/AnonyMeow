using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

// Postgres native full-text search (websearch_to_tsquery + ts_rank against the generated
// SearchVector columns) — no external search service, per the locked-in plan decision.
public class SearchService(AppDbContext dbContext) : ISearchService
{
    private const string TsConfig = "english";

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> SearchPostsAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Title is a short field, so it also gets bidirectional ILike matching (both
        // title-contains-query and query-contains-title) alongside the lexeme-based full-text
        // match — catches partial words/compounds the tsvector match would otherwise miss. Body
        // text stays full-text-only (long text, reverse-contains wouldn't make sense there).
        var pattern = $"%{query}%";
        var matches = dbContext.Posts.Where(p =>
            !p.IsRemoved &&
            (p.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery(TsConfig, query)) ||
             EF.Functions.ILike(p.Title, pattern) ||
             EF.Functions.ILike(query, "%" + p.Title + "%")));

        var totalCount = await matches.CountAsync(cancellationToken);
        var items = await matches
            .OrderByDescending(p => p.SearchVector!.Rank(EF.Functions.WebSearchToTsQuery(TsConfig, query)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Comment> Items, int TotalCount)> SearchCommentsAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var matches = dbContext.Comments.Where(c =>
            !c.IsRemoved && c.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery(TsConfig, query)));

        var totalCount = await matches.CountAsync(cancellationToken);
        var items = await matches
            .OrderByDescending(c => c.SearchVector!.Rank(EF.Functions.WebSearchToTsQuery(TsConfig, query)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Community> Items, int TotalCount)> SearchCommunitiesAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Name is a short field, so it also gets bidirectional ILike matching alongside the
        // full-text match — see SearchPostsAsync's comment for why.
        var pattern = $"%{query}%";
        var matches = dbContext.Communities.Where(c =>
            c.SearchVector!.Matches(EF.Functions.WebSearchToTsQuery(TsConfig, query)) ||
            EF.Functions.ILike(c.Name, pattern) ||
            EF.Functions.ILike(query, "%" + c.Name + "%"));

        var totalCount = await matches.CountAsync(cancellationToken);
        var items = await matches
            .OrderByDescending(c => c.SearchVector!.Rank(EF.Functions.WebSearchToTsQuery(TsConfig, query)))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
