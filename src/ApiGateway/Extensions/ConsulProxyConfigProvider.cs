using Consul;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Linq;

using YarpRouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using YarpClusterConfig = Yarp.ReverseProxy.Configuration.ClusterConfig;
using YarpDestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;

public class ConsulProxyConfigProvider : IProxyConfigProvider
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulProxyConfigProvider> _logger;
    private volatile CustomProxyConfig _config;
    private CancellationTokenSource _cts = new();

    public ConsulProxyConfigProvider(
        IConsulClient consulClient,
        ILogger<ConsulProxyConfigProvider> logger)
    {
        _consulClient = consulClient;
        _logger = logger;

        _config = new CustomProxyConfig(
            new List<YarpRouteConfig>(),
            new List<YarpClusterConfig>(),
            _cts.Token
        );

        _ = Task.Run(UpdateConfigPeriodically);
    }

    private async Task UpdateConfigPeriodically()
    {
        while (true)
        {
            try
            {
                var services = await _consulClient.Agent.Services();
                
                var serviceGroups = services.Response.Values
                    .GroupBy(svc => svc.Service)
                    .ToList();

                var clusters = new List<YarpClusterConfig>();
                var routes = new List<YarpRouteConfig>();

                foreach (var group in serviceGroups)
                {
                    string serviceName = group.Key;
                    
                    if (serviceName.Equals("apigateway", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var destinations = new Dictionary<string, YarpDestinationConfig>();
                    
                    foreach (var svc in group)
                    {
                        // 🔧 host.docker.internal -> localhost dönüştür
                        var address = svc.Address == "host.docker.internal" 
                            ? "127.0.0.1" 
                            : svc.Address;
                        
                        var destinationAddress = $"http://{address}:{svc.Port}";
                        
                        destinations[svc.ID] = new YarpDestinationConfig
                        {
                            Address = destinationAddress
                        };
                        
                        _logger.LogInformation($"🎯 {serviceName} [{svc.ID}] -> {destinationAddress}");
                    }

                    clusters.Add(new YarpClusterConfig
                    {
                        ClusterId = serviceName,
                        Destinations = destinations,
                        LoadBalancingPolicy = "RoundRobin"
                    });

                    // 👇 BURASI DEĞİŞTİ - PathPattern transform kullanıyor
                    routes.Add(new YarpRouteConfig
                    {
                        RouteId = $"{serviceName}-route",
                        ClusterId = serviceName,
                        Match = new RouteMatch
                        {
                            Path = $"/{serviceName}/{{**catch-all}}"  // /api kaldırıldı
                        },
                        Transforms = new[]
                        {
                            new Dictionary<string, string>
                            {
                                ["PathPattern"] = "/api/{**catch-all}"
                            },
                            new Dictionary<string, string>
                            {
                                ["RequestHeadersCopy"] = "true"
                            },
                            new Dictionary<string, string>
                            {
                                ["RequestHeaderOriginalHost"] = "true"
                            }
                        }
                    });
                    
                    _logger.LogInformation($"🔀 Route eklendi: {serviceName}-route -> /{serviceName}/{{**catch-all}} => /api/{{**catch-all}}");
                }

                var oldCts = _cts;
                _cts = new CancellationTokenSource();
                _config = new CustomProxyConfig(routes, clusters, _cts.Token);
                oldCts?.Cancel();

                _logger.LogInformation(
                    $"✅ YARP config güncellendi: {routes.Count} servis, " +
                    $"{clusters.Sum(c => c.Destinations?.Count ?? 0)} instance");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Consul sync hatası");
            }

            await Task.Delay(5000);
        }
    }
  
    public IProxyConfig GetConfig() => _config;
}

public class CustomProxyConfig : IProxyConfig
{
    public IReadOnlyList<YarpRouteConfig> Routes { get; }
    public IReadOnlyList<YarpClusterConfig> Clusters { get; }
    public IChangeToken ChangeToken { get; }

    public CustomProxyConfig(
        IReadOnlyList<YarpRouteConfig> routes,
        IReadOnlyList<YarpClusterConfig> clusters,
        CancellationToken changeToken)
    {
        Routes = routes;
        Clusters = clusters;
        ChangeToken = new CancellationChangeToken(changeToken);
    }
}