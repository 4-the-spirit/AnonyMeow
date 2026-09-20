using AnonyMeow.Common.Middleware;
using AnonyMeow.Dtos.Flairs;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AnonyMeow.Endpoints;

public static class FlairEndpoints
{
    public static IEndpointRouteBuilder MapFlairEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/communities/{name}/flairs");

        group.MapPost("/", CreateFlairAsync).WithName("CreateFlair");
        group.MapGet("/", ListFlairsAsync).WithName("ListFlairs").AllowAnonymous();
        group.MapPatch("/{id:guid}", UpdateFlairAsync).WithName("UpdateFlair");
        group.MapDelete("/{id:guid}", DeleteFlairAsync).WithName("DeleteFlair");

        return app;
    }

    private static async Task<Results<Created<FlairResponse>, NotFound, ForbidHttpResult, JsonHttpResult<HttpValidationProblemDetails>>> CreateFlairAsync(
        string name,
        CreateFlairRequest request,
        ICommunityService communityService,
        IFlairService flairService,
        ICurrentUserAccessor currentUserAccessor,
        IAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(httpContext.User, community, "CommunityModerator");
        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblemFactory.Create("name", "Flair name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ColorHex))
        {
            return ValidationProblemFactory.Create("colorHex", "Flair color is required.");
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var flair = await flairService.CreateAsync(
            community.Id, request.Name, request.ColorHex, currentUser.Id, cancellationToken);

        return TypedResults.Created(
            $"/api/communities/{Uri.EscapeDataString(name)}/flairs/{flair.Id}", FlairResponse.FromEntity(flair));
    }

    private static async Task<Results<Ok<IReadOnlyList<FlairResponse>>, NotFound>> ListFlairsAsync(
        string name,
        ICommunityService communityService,
        IFlairService flairService,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var flairs = await flairService.ListForCommunityAsync(community.Id, cancellationToken);
        return TypedResults.Ok<IReadOnlyList<FlairResponse>>(flairs.Select(FlairResponse.FromEntity).ToList());
    }

    private static async Task<Results<Ok<FlairResponse>, NotFound, ForbidHttpResult, JsonHttpResult<HttpValidationProblemDetails>>> UpdateFlairAsync(
        string name,
        Guid id,
        UpdateFlairRequest request,
        ICommunityService communityService,
        IFlairService flairService,
        IAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(httpContext.User, community, "CommunityModerator");
        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationProblemFactory.Create("name", "Flair name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ColorHex))
        {
            return ValidationProblemFactory.Create("colorHex", "Flair color is required.");
        }

        var flair = await flairService.GetByIdAsync(id, cancellationToken);
        if (flair is null || flair.CommunityId != community.Id)
        {
            return TypedResults.NotFound();
        }

        var updated = await flairService.UpdateAsync(flair, request.Name, request.ColorHex, cancellationToken);
        return TypedResults.Ok(FlairResponse.FromEntity(updated));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteFlairAsync(
        string name,
        Guid id,
        ICommunityService communityService,
        IFlairService flairService,
        IAuthorizationService authorizationService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(httpContext.User, community, "CommunityModerator");
        if (!authResult.Succeeded)
        {
            return TypedResults.Forbid();
        }

        var flair = await flairService.GetByIdAsync(id, cancellationToken);
        if (flair is null || flair.CommunityId != community.Id)
        {
            return TypedResults.NotFound();
        }

        await flairService.DeleteAsync(flair, cancellationToken);
        return TypedResults.NoContent();
    }
}
