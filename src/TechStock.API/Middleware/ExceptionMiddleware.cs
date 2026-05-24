using System.Net;
using System.Text.Json;
using TechStock.Application.Exceptions;

namespace TechStock.API.Middleware;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (status, message) = ex switch
        {
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            BusinessException => (HttpStatusCode.BadRequest, ex.Message),
            Application.Exceptions.ValidationException ve =>
                (HttpStatusCode.UnprocessableEntity, JsonSerializer.Serialize(ve.Errors)),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        context.Response.StatusCode = (int)status;

        var payload = ex is Application.Exceptions.ValidationException
            ? message
            : JsonSerializer.Serialize(new { error = message });

        return context.Response.WriteAsync(payload);
    }
}
