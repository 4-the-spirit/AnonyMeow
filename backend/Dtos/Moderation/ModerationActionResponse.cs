using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Moderation;

public record ModerationActionResponse(Guid Id, ModerationActionType ActionType, Guid TargetId, string? Reason, DateTimeOffset CreatedAtUtc);
