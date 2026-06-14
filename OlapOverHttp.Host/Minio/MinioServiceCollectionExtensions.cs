using Minio;
using OlapOverHttp.Host.Minio;

namespace OlapOverHttp.Host.Minio;

public static class MinioServiceCollectionExtensions
{
    public static IServiceCollection AddMinio(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>();

        if (options is null)
            throw new ArgumentNullException(nameof(options), "Minio must be configured");

        services.AddSingleton(options);

        services.AddMinio(configureClient =>
        {
            configureClient
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl);
        });

        services.AddHostedService<MinioBucketInitializer>();

        return services;
    }
}
