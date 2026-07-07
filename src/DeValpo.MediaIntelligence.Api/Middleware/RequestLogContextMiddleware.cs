using DeValpo.MediaIntelligence.Api.Auth;
using Serilog.Context;

namespace DeValpo.MediaIntelligence.Api.Middleware;

public sealed class RequestLogContextMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Adds request and authenticated client metadata to the structured log context.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var clientKey = context.User.FindFirst(AuthClaimTypes.ClientKey)?.Value;

        using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        using (LogContext.PushProperty("RequestPath", context.Request.Path.Value))
        using (LogContext.PushProperty("ClientKey", string.IsNullOrWhiteSpace(clientKey) ? null : clientKey))
        {
            await next(context);
        }
    }
}
