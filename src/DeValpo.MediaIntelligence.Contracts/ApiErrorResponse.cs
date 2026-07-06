namespace DeValpo.MediaIntelligence.Contracts;

public sealed record ApiErrorResponse(
    string Code,
    string Message,
    string TraceId);
