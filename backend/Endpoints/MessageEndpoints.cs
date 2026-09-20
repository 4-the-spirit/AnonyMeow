using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Moderation;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AnonyMeow.Endpoints;

public static class MessageEndpoints
{
    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/messages/{id:guid}/reports", CreateMessageReportAsync).WithName("CreateMessageReport").RequireRateLimiting("Report");

        return app;
    }

    private static async Task<Results<Created<ReportResponse>, NotFound>> CreateMessageReportAsync(
        Guid id,
        CreateReportRequest request,
        IMessageService messageService,
        IConversationService conversationService,
        IReportService reportService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var message = await messageService.GetByIdAsync(id, cancellationToken);
        if (message is null)
        {
            return TypedResults.NotFound();
        }

        var conversation = await conversationService.GetByIdAsync(message.ConversationId, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (conversation is null || !conversationService.IsParticipant(conversation, currentUser.Id))
        {
            return TypedResults.NotFound();
        }

        var report = await reportService.CreateAsync(
            ReportTargetType.DirectMessage, id, currentUser.Id, request.Category, request.Details, cancellationToken);
        return TypedResults.Created($"/api/messages/{id}/reports/{report.Id}", ReportResponse.FromEntity(report));
    }
}
