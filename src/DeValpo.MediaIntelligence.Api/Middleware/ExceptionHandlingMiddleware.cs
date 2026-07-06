using DeValpo.MediaIntelligence.Api.Resources;
using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    /// <summary>
    /// Converts known application exceptions into stable JSON error responses.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ArgumentException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "INVALID_REQUEST", exception.Message, exception);
        }
        catch (InvalidOperationException exception)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "REQUEST_NOT_PROCESSABLE", exception.Message, exception);
        }
        catch (Exception exception)
        {
            var message = environment.IsDevelopment()
                ? exception.Message
                : ApiResourceText.UnexpectedError;

            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", message, exception);
        }
    }

    /// <summary>
    /// Writes a structured API error response and logs the original exception.
    /// </summary>
    private async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        Exception exception)
    {
        logger.LogError(exception, ApiResourceText.RequestFailedLogMessage, code);

        if (context.Response.HasStarted)
        {
            throw exception;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse(code, message, context.TraceIdentifier);
        await context.Response.WriteAsJsonAsync(response);
    }
}
