namespace AnonyMeow.Common.Exceptions;

public class SoleModeratorLeaveException(string communityName)
    : ApiException(
        StatusCodes.Status409Conflict,
        "Sole Moderator Cannot Leave",
        $"'{communityName}' has no other moderators; promote another member before leaving.");
