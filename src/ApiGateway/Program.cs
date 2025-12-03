using Consul;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);

// 👇 Consul JSON Configuration
builder.Configuration.AddConsulJson(
    consulKey: "config/jwt-settings",
    consulAddress: "http://localhost:8500"
);

// Rate Limiting
// status code 429 olsun, kullanıcıya bildir ve kullanıcıya ne kadar beklemesi gerektiğini söyle
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return RateLimitPartition.GetFixedWindowLimiter("Global", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1, // max 1 istek
            Window = TimeSpan.FromMinutes(1), // 1 dakika penceresi
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0 // Kuyruk yok
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = 429; // Too Many Requests
        context.HttpContext.Response.Headers.RetryAfter = "60"; // 60 saniye sonra tekrar dene
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken);
    };
});

// --------------------------------------
// Consul Service Discovery (senin extension)
// --------------------------------------
builder.Services.AddConsulServiceDiscovery(builder.Configuration);

// --------------------------------------
// YARP - Dinamik Config Provider
// --------------------------------------
// YARP
builder.Services.AddReverseProxy();

builder.Services.AddSingleton<IProxyConfigProvider, ConsulProxyConfigProvider>();

// --------------------------------------
// Consul Client
// --------------------------------------
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p =>
    new ConsulClient(cfg => cfg.Address = new Uri("http://localhost:8500")));

// --------------------------------------
// JWT Authentication
// --------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key configuration is missing!");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "exampleIssuer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "exampleAudience";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            )
        };
    });

// 🔐 Authorization Policies - Role
/*
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("Admin"));
    
    options.AddPolicy("UserOrAdmin", policy => 
        policy.RequireRole("User", "Admin"));
    
    options.AddPolicy("AllRoles", policy => 
        policy.RequireAuthenticatedUser());
});
*/

// Route-based Authorization Service
builder.Services.AddSingleton<IRouteAuthorizationService, RouteAuthorizationService>();


builder.Services.AddAuthorization();


// --------------------------------------
// BUILD
// --------------------------------------
var app = builder.Build();

// Logging ekle
builder.Logging.AddConsole();

// Health check'leri geliştir
app.MapGet("/health", async (IConsulClient consul) =>
{
    try
    {
        await consul.Agent.Services();
        return Results.Ok(new { status = "healthy", consul = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "degraded", consul = "disconnected", error = ex.Message });
    }
});

app.UseCors("AllowAll");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// 🔐 Custom Authorization Middleware - Role
// app.UseMiddleware<RouteAuthorizationMiddleware>();

// YARP endpoint
app.MapReverseProxy();

app.Run();
