namespace AnonyMeow.Domain;

public class UserBlock
{
    public Guid BlockerId { get; set; }
    public Guid BlockedId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
