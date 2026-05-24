using System.Security.Claims;
using TechStock.Application.Interfaces;

namespace TechStock.API.Middleware;

public class AuditMiddleware
{
    private readonly RequestDelegate _next;

    public AuditMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAuditService auditService)
    {
        await _next(context);

        var method = context.Request.Method;
        if (!HttpMethods.IsPost(method) && !HttpMethods.IsPut(method) && !HttpMethods.IsDelete(method))
            return;

        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
            return;

        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdStr, out var userId)) return;

        var action = method switch
        {
            "POST" => "Created",
            "PUT" => "Updated",
            "DELETE" => "Deleted",
            _ => method
        };

        var path = context.Request.Path.Value ?? "";
        var segments = path.Trim('/').Split('/');
        var entityType = segments.Length >= 2 ? segments[1] : path;

        if (Guid.TryParse(segments.LastOrDefault(), out var entityId))
            await auditService.LogAsync(userId, action, entityType, entityId);
    }
}
