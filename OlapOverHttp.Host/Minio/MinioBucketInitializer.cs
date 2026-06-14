using Minio;
using Minio.DataModel.Args;

namespace OlapOverHttp.Host.Minio;

internal sealed class MinioBucketInitializer(
    IMinioClient minioClient,
    MinioOptions options,
    ILogger<MinioBucketInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucketName = options.BucketName;
        var exists = await minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(bucketName),
            cancellationToken);

        if (exists)
        {
            logger.LogInformation("MinIO bucket '{BucketName}' already exists.", bucketName);
            return;
        }

        await minioClient.MakeBucketAsync(
            new MakeBucketArgs().WithBucket(bucketName),
            cancellationToken);

        logger.LogInformation("Created MinIO bucket '{BucketName}'.", bucketName);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
