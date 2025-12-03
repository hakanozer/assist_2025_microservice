public class RouteAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RouteAuthorizationMiddleware> _logger;

    public RouteAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<RouteAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IRouteAuthorizationService authService)
    {
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // Extract service name from path: /product-service/... -> product-service
        var serviceName = ExtractServiceName(path);

        if (!string.IsNullOrEmpty(serviceName))
        {
            var isAuthorized = authService.IsAuthorized(
                serviceName,
                method,
                path,
                context.User
            );

            if (!isAuthorized)
            {
                _logger.LogWarning($"🚫 Authorization failed: {method} {path}");
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Forbidden",
                    message = "You don't have permission to access this resource"
                });
                return;
            }
        }

        await _next(context);
    }

    private string? ExtractServiceName(string path)
    {
        // /product-service/Product/all -> product-service
        // /api/product-service/... -> product-service
        
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts.Length > 0)
        {
            var firstPart = parts[0];
            if (firstPart == "api" && parts.Length > 1)
            {
                return parts[1]; // /api/product-service
            }
            return firstPart; // /product-service
        }

        return null;
    }
}