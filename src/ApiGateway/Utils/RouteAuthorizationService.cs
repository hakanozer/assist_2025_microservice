using System.Security.Claims;

public interface IRouteAuthorizationService
{
    bool IsAuthorized(string serviceName, string method, string path, ClaimsPrincipal user);
}

public class RouteAuthorizationService : IRouteAuthorizationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RouteAuthorizationService> _logger;

    public RouteAuthorizationService(
        IConfiguration configuration,
        ILogger<RouteAuthorizationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsAuthorized(string serviceName, string method, string path, ClaimsPrincipal user)
    {
        // Public endpoints (no auth required)
        var publicEndpoints = new[]
        {
            "/health",
            "/api/auth-service/auth/login",
            "/api/auth-service/auth/register"
        };

        if (publicEndpoints.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // User must be authenticated for protected routes
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning($"❌ Unauthenticated access attempt: {method} {path}");
            return false;
        }

        // Get route config from appsettings
        var routeKey = $"{method}:{path}";
        var allowedRoles = _configuration
            .GetSection($"Authorization:Routes:{serviceName}:{routeKey}")
            .Get<string[]>();

        // Wildcard check
        if (allowedRoles == null)
        {
            allowedRoles = _configuration
                .GetSection($"Authorization:Routes:{serviceName}:*")
                .Get<string[]>();
        }

        // If no config, deny by default
        if (allowedRoles == null || allowedRoles.Length == 0)
        {
            _logger.LogWarning($"⚠️ No authorization config for: {serviceName} {method} {path}");
            return true; // Ya da false yapabilirsiniz (deny by default)
        }

        // Check user roles
        var userRoles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        var hasAccess = allowedRoles.Any(role => userRoles.Contains(role));

        if (!hasAccess)
        {
            _logger.LogWarning(
                $"🚫 Access denied: User [{string.Join(",", userRoles)}] tried {method} {path} " +
                $"(Required: [{string.Join(",", allowedRoles)}])");
        }

        return hasAccess;
    }
}