using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Moderation;

public record ResolveReportRequest(ReportOutcome Outcome, string? ActionReason);
