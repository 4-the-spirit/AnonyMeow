namespace AnonyMeow.Domain;

public class PollOption
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public required string Text { get; set; }
}
