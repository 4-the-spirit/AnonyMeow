namespace AnonyMeow.Dtos.Messages;

public record SendMessageRequest(string Body, Guid? ReplyToMessageId = null);
