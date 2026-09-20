using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Domain;

public class Vote
{
    public Guid Id { get; set; }
    public VoteTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public Guid VoterId { get; set; }
    public sbyte Value { get; set; }
}
