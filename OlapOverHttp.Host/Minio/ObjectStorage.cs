using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace OlapOverHttp.Host.Minio;

public sealed class ObjectStorage(
    MinioOptions options,
    IMinioClient minio)
{
    private readonly string _bucketName = options.BucketName;
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Stream?> Download(string objectName)
    {
		try
		{
            await minio.StatObjectAsync(new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName));

            var responseStream = new MemoryStream();

            await minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(async (stream, cancellationToken) =>
                {
                    await stream.CopyToAsync(responseStream, cancellationToken);
                }));

            responseStream.Position = 0;
            return responseStream;
        }
        catch (MinioException)
        {
            return null;
		}
    }

    public Task Upload(string objectName, Stream stream)
        => minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(GetObjectSize(stream))
            .WithContentType(ExcelContentType));

    private static long GetObjectSize(Stream stream)
        => stream.CanSeek ? stream.Length : -1;
}
