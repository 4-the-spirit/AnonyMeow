using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;

namespace AnonyMeow.Common.Authorization;

public class ProfileCompletionHandler(ICurrentUserAccessor currentUserAccessor)
    : AuthorizationHandler<ProfileCompletionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProfileCompletionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await currentUserAccessor.GetCurrentUserAsync();
        if (!string.IsNullOrEmpty(user.Username))
        {
            context.Succeed(requirement);
        }
    }
}
