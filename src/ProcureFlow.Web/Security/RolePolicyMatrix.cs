using ProcureFlow.Core.Constants;

namespace ProcureFlow.Web.Security;

/// <summary>
/// Centralized mapping of HTTP method + route pattern to allowed roles.
/// Middleware uses this to enforce deny-by-default access control.
/// </summary>
public static class RolePolicyMatrix
{
    // Key: (method, prefix) — method is the HTTP verb (uppercase), prefix is the path prefix to match.
    // Value: allowed roles (any match = allow).
    private static readonly List<(string Method, string PathPrefix, string[] AllowedRoles)> _policies =
    [
        // Admin-only write endpoints
        ("POST",  "/api/admin",  [Roles.Admin]),
        ("PATCH", "/api/admin",  [Roles.Admin]),
        ("PUT",   "/api/admin",  [Roles.Admin]),
        ("DELETE","/api/admin",  [Roles.Admin]),

        // Admin read-only is also restricted to Admin (listing employees, companies is internal)
        ("GET",   "/api/admin",  [Roles.Admin]),

        // Master data is readable by Admin and Buyer; Vendors are denied
        ("GET",   "/api/master-data", [Roles.Admin, Roles.Buyer]),

        // Buyer endpoints — Admin and Buyer can access
        ("POST",  "/api/buyer",  [Roles.Admin, Roles.Buyer]),
        ("PATCH", "/api/buyer",  [Roles.Admin, Roles.Buyer]),
        ("GET",   "/api/buyer",  [Roles.Admin, Roles.Buyer]),

        // Vendor endpoints — Admin and Vendor can access
        ("POST",  "/api/vendor", [Roles.Admin, Roles.Vendor]),
        ("PATCH", "/api/vendor", [Roles.Admin, Roles.Vendor]),
        ("GET",   "/api/vendor", [Roles.Admin, Roles.Vendor]),
    ];

    /// <summary>
    /// Returns whether the given role is allowed to access the endpoint.
    /// Returns null when no policy matches (defer to ASP.NET authorization).
    /// </summary>
    public static PolicyVerdict Evaluate(string method, string path, string? role)
    {
        foreach (var (m, prefix, allowedRoles) in _policies)
        {
            if (!string.Equals(m, method, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            // Policy found — check role
            if (role is null)
                return PolicyVerdict.Unauthorized;

            return allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase)
                ? PolicyVerdict.Allow
                : PolicyVerdict.Forbidden;
        }

        return PolicyVerdict.NoPolicy;
    }
}

public enum PolicyVerdict
{
    Allow,
    Forbidden,
    Unauthorized,
    NoPolicy,
}
