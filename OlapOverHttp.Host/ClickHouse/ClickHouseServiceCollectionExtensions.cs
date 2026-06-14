using Octonica.ClickHouseClient;

namespace OlapOverHttp.Host.ClickHouse;

public static class ClickHouseServiceCollectionExtensions
{
    private const string ConnectionStringEnvVar = "CLICKHOUSE_CONNECTION_STRING";

    public static IServiceCollection AddClickHouse(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ClickHouseOptions.SectionName).Get<ClickHouseOptions>();

        if (options is null)
            throw new ArgumentNullException(nameof(options), "Clickhouse options must be configured");

        var connectionString = options.ConnectionString;

        var settings = new ClickHouseConnectionStringBuilder(connectionString).BuildSettings();

        services.AddSingleton(settings);
        services.AddSingleton<IClickHouseConnectionFactory, ClickHouseConnectionFactory>();

        return services;
    }
}
