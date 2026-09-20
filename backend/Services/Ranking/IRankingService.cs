using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services.Ranking;

public interface IRankingService
{
    IQueryable<Post> ApplyPostSort(IQueryable<Post> posts, SortOrder sortOrder);

    IQueryable<Comment> ApplyCommentSort(IQueryable<Comment> comments, SortOrder sortOrder);
}
