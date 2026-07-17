namespace LLM_Demo.Agents.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

public static class OrleansConfigurator
{
    public static ISiloBuilder ConfigureOrleansSilo(this ISiloBuilder builder, string connectionString)
    {
        builder
            .UseLocalhostClustering()
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "LLM_Demo";
                options.ServiceId = "LLM_Demo_Service";
            })
            .AddAdoNetGrainStorage("AgentStorage", options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            })
            .AddAdoNetGrainStorage("ConversationStorage", options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            })
            .UseAdoNetClustering(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = connectionString;
            });

        return builder;
    }

    public static IHostBuilder AddOrleansClient(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseOrleansClient(client =>
        {
            client.UseLocalhostClustering();
        });

        return hostBuilder;
    }
}
