using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace AnonyMeow.Services.Ranking;

public class RankingService(IServiceProvider serviceProvider) : IRankingService
{
    public IQueryable<Post> ApplyPostSort(IQueryable<Post> posts, SortOrder sortOrder) =>
        serviceProvider.GetRequiredKeyedService<ISortStrategy>(sortOrder).ApplyToPosts(posts);

    public IQueryable<Comment> ApplyCommentSort(IQueryable<Comment> comments, SortOrder sortOrder) =>
        serviceProvider.GetRequiredKeyedService<ISortStrategy>(sortOrder).ApplyToComments(comments);
}
