using OlapOverHttp.Host.Extensions;
using OlapOverHttp.Host.Minio;
using System.IO.Pipelines;

namespace OlapOverHttp.Host.Excel;

public sealed class CachedPostingReportGenerator(
    PostingExcelBuilder excelBuilder,
    ObjectStorage objectStorage)
{
    public async Task GenerateCachedReport(
        ReportRequest request,
        PipeWriter writer,
        CancellationToken token)
    {
        var reportObjectName = request.GetObjectName();

        var reportStream = await objectStorage.Download(reportObjectName)
            ?? await GenerateCachedReportAndDownload(request, token);

        if (reportStream is not null)
            await reportStream.CopyToAsync(writer);
        else
            await GenerateReport(request, writer, token);
    }

    private async Task<Stream?> GenerateCachedReportAndDownload(ReportRequest request, CancellationToken token)
    {
        try
        {
            await GenerateCachedReport(request, token);
        }
        catch (Exception e)
        {
            return null;
        }

        return await objectStorage.Download(request.GetObjectName());
    }

    private async Task GenerateCachedReport(ReportRequest request, CancellationToken token)
    {
        var reportObjectName = request.GetObjectName();
        var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 512 * 1024, resumeWriterThreshold: 256 * 1024));

        await GenerateReport(request, pipe.Writer, token);
        await objectStorage.Upload(reportObjectName, pipe.Reader.AsStream());
    }

    private Task GenerateReport(ReportRequest request, PipeWriter writer, CancellationToken token)
        => excelBuilder
            .Build(
                request.SellerId,
                request.PeriodStart,
                request.PeriodEnd,
                writer.AsStream(),
                token)
            .ContinueWith(
                t => writer.Complete(t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Current);
}
