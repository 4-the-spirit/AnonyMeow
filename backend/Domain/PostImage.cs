namespace AnonyMeow.Domain;

public class PostImage
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public required string Url { get; set; }
    public int Position { get; set; }
}
