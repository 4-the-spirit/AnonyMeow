using AnonyMeow.Domain;

namespace AnonyMeow.Services.Ranking;

public interface ISortStrategy
{
    IQueryable<Post> ApplyToPosts(IQueryable<Post> posts);

    IQueryable<Comment> ApplyToComments(IQueryable<Comment> comments);
}
