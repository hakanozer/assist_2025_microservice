using Consul;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.IO;
using System;

public static class ConsulConfigurationExtensions
{
    public static IConfigurationBuilder AddConsulJson(
        this IConfigurationBuilder builder,
        string consulKey,
        string consulAddress)
    {
        var consulClient = new ConsulClient(cfg =>
        {
            cfg.Address = new Uri(consulAddress);
        });

        var kv = consulClient.KV.Get(consulKey).Result;

        if (kv?.Response?.Value == null)
            throw new Exception($"Consul key not found: {consulKey}");

        string json = Encoding.UTF8.GetString(kv.Response.Value);
        Console.WriteLine($"Loaded configuration json: {json}");
        builder.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));

        return builder;
    }
}
