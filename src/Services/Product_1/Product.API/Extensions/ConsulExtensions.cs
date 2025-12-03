using Consul;

public static class ConsulExtensions
{

    public class ConsulConfig
    {
        public string Address { get; set; }
        public string ServiceName { get; set; }
        public string ServiceAddress { get; set; }
        public int ServicePort { get; set; }
    }
    public static IServiceCollection AddConsulServiceDiscovery(
        this IServiceCollection services, IConfiguration configuration)
    {
        var consulConfig = configuration.GetSection("Consul").Get<ConsulConfig>();
        services.AddSingleton<IConsulClient, ConsulClient>(p =>
            new ConsulClient(cfg => cfg.Address = new Uri(consulConfig.Address)));
        services.AddHostedService<ConsulHostedService>();
        return services;
    }

    public class ConsulHostedService : IHostedService
{
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private string _registrationId;

    public ConsulHostedService(IConsulClient consulClient, IConfiguration configuration)
    {
        _consulClient = consulClient;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceName = _configuration["Consul:ServiceName"];
        var serviceId = $"{serviceName}-{Guid.NewGuid()}";
        _registrationId = serviceId;
        var url = $"http://{_configuration["Consul:ServiceAddress"]}:{_configuration["Consul:ServicePort"]}/health";
        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = _configuration["Consul:ServiceAddress"],
            Port = int.Parse(_configuration["Consul:ServicePort"]),
            Check = new AgentServiceCheck
            {
                HTTP = url,
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
                
            }
        };

        await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
        await _consulClient.Agent.ServiceRegister(registration, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _consulClient.Agent.ServiceDeregister(_registrationId, cancellationToken);
    }
}



}
