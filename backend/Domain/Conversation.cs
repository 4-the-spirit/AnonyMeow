namespace AnonyMeow.Domain;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid ParticipantAId { get; set; }
    public Guid ParticipantBId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    // Pin/delete are per-participant, not global — each side of a conversation can pin or hide
    // it independently, so state is tracked as a pair of nullable timestamps rather than a
    // single shared flag.
    public DateTimeOffset? PinnedByAAtUtc { get; set; }
    public DateTimeOffset? PinnedByBAtUtc { get; set; }
    public DateTimeOffset? DeletedByAAtUtc { get; set; }
    public DateTimeOffset? DeletedByBAtUtc { get; set; }

    public bool IsPinnedBy(Guid userId) =>
        userId == ParticipantAId ? PinnedByAAtUtc.HasValue : userId == ParticipantBId && PinnedByBAtUtc.HasValue;

    public bool IsDeletedBy(Guid userId) =>
        userId == ParticipantAId ? DeletedByAAtUtc.HasValue : userId == ParticipantBId && DeletedByBAtUtc.HasValue;
}
