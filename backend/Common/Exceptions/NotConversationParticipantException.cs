namespace AnonyMeow.Common.Exceptions;

// 404, not 403 — a non-participant shouldn't be able to confirm a conversation exists at all,
// same reasoning as the notification-ownership 404 in NotificationEndpoints.
public class NotConversationParticipantException()
    : ApiException(StatusCodes.Status404NotFound, "Conversation Not Found", "The requested conversation does not exist.");
