using System.Security.Claims;
using ProcureFlow.Web.Security;

namespace ProcureFlow.Web.Middleware;

public class RbacPolicyMiddleware
{
    private readonly RequestDelegate _next;

    public RbacPolicyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var role = context.User.FindFirstValue(ClaimTypes.Role);

        var verdict = RolePolicyMatrix.Evaluate(method, path, role);

        switch (verdict)
        {
            case PolicyVerdict.Allow:
                await _next(context);
                break;

            case PolicyVerdict.Unauthorized:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { code = "UNAUTHORIZED" });
                break;

            case PolicyVerdict.Forbidden:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { code = "FORBIDDEN" });
                break;

            case PolicyVerdict.NoPolicy:
                // No matching policy — let ASP.NET authorization pipeline handle it
                await _next(context);
                break;
        }
    }
}

public static class RbacPolicyMiddlewareExtensions
{
    public static IApplicationBuilder UseRbacPolicy(this IApplicationBuilder app)
        => app.UseMiddleware<RbacPolicyMiddleware>();
}
