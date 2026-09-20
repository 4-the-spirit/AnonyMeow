namespace AnonyMeow.Common.Exceptions;

public class CommunityMemberNotFoundException()
    : ApiException(StatusCodes.Status404NotFound, "Community Member Not Found", "This user is not a member of the community.");
