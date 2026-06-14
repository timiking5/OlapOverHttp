namespace OlapOverHttp.Host.Postgres;

public static class PostgresServiceCollectionExtensions
{
    public static IServiceCollection AddPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>();

        if (options is null)
            throw new ArgumentNullException(nameof(options), "Postgres options must be configured");

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new InvalidOperationException("Postgres connection string must be configured");

        services.AddSingleton(options);
        services.AddSingleton<IPostgresConnectionFactory>(_ => new PostgresConnectionFactory(options.ConnectionString));

        return services;
    }
}
