namespace AnonyMeow.Common.Exceptions;

public class SelfConversationException()
    : ApiException(StatusCodes.Status403Forbidden, "Cannot Message Yourself", "You cannot start a conversation with yourself.");
