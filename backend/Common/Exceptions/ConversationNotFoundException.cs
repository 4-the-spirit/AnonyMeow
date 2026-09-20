namespace AnonyMeow.Common.Exceptions;

public class ConversationNotFoundException()
    : ApiException(StatusCodes.Status404NotFound, "Conversation Not Found", "The requested conversation does not exist.");
