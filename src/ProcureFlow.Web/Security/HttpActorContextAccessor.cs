using System.Security.Claims;
using ProcureFlow.Infrastructure.Data.Interceptors;

namespace ProcureFlow.Web.Security;

/// <summary>
/// Resolves the current actor from the HTTP context for audit stamping.
/// </summary>
public sealed class HttpActorContextAccessor : IActorContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpActorContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string Actor =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
}
