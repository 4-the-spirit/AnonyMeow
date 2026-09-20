using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Moderation;

public record CreateReportRequest(ReportReasonCategory Category, string? Details);
