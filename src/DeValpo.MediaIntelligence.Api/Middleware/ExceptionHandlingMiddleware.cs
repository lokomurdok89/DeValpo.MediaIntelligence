using DeValpo.MediaIntelligence.Contracts;

namespace DeValpo.MediaIntelligence.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
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
                : "An unexpected error occurred.";

            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", message, exception);
        }
    }

    private async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string code,
        string message,
        Exception exception)
    {
        logger.LogError(exception, "Request failed with {ErrorCode}", code);

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
