using BizSpace.Dtos;
using BizSpace.Exceptions;
using BizSpace.Services.Images;
using Microsoft.EntityFrameworkCore;

namespace BizSpace.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled error occurred.");
            var statusCode = exception switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedException => StatusCodes.Status401Unauthorized,
                ForbiddenException => StatusCodes.Status403Forbidden,
                ArgumentException => StatusCodes.Status400BadRequest,
                BusinessRuleException => StatusCodes.Status409Conflict,
                ImageStorageNotConfiguredException => StatusCodes.Status503ServiceUnavailable,
                ImageStorageException => StatusCodes.Status502BadGateway,
                DbUpdateException => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var message = statusCode == StatusCodes.Status500InternalServerError
                ? "An unexpected error occurred."
                : exception.Message;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message));
        }
    }
}
