namespace AnonyMeow.Domain;

public class PollVote
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid AppUserId { get; set; }
    public Guid PollOptionId { get; set; }
}
