using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;

namespace AnonyMeow.Common.Authorization;

public class PlatformAdminHandler(ICurrentUserAccessor currentUserAccessor) : AuthorizationHandler<PlatformAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdminRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await currentUserAccessor.GetCurrentUserAsync();
        if (user.IsPlatformAdmin)
        {
            context.Succeed(requirement);
        }
    }
}
