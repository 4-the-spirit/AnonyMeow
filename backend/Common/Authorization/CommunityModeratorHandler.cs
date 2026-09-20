using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Common.Authorization;

public class CommunityModeratorHandler(ICurrentUserAccessor currentUserAccessor, AppDbContext dbContext)
    : AuthorizationHandler<CommunityModeratorRequirement, Community>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CommunityModeratorRequirement requirement,
        Community resource)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await currentUserAccessor.GetCurrentUserAsync();
        var isModerator = await dbContext.CommunityMemberships.AnyAsync(m =>
            m.CommunityId == resource.Id && m.AppUserId == user.Id && m.Role == CommunityRole.Moderator);

        if (isModerator)
        {
            context.Succeed(requirement);
        }
    }
}
