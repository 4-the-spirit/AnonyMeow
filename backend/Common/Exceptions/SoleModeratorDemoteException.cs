namespace AnonyMeow.Common.Exceptions;

public class SoleModeratorDemoteException(string communityName)
    : ApiException(
        StatusCodes.Status409Conflict,
        "Sole Moderator Cannot Be Demoted",
        $"'{communityName}' has no other moderators; promote another member before demoting this one.");
