using OlapOverHttp.Host.Data;
using OlapOverHttp.Host.Excel;
using OlapOverHttp.Host.Extensions;

namespace OlapOverHttp.Host.Endpoints;

public static class PostingEndpoints
{
    public static IEndpointRouteBuilder MapPostingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/postings");

        group.MapGet("/", GetPostings);

        group.MapGet("/report", DownloadReport);
        group.MapGet("/cached-report", DownloadCachedReport);

        return endpoints;
    }

    private static IResult GetPostings(
        long sellerId,
        DateOnly from,
        DateOnly to,
        IPostingRepository repository,
        CancellationToken cancellationToken) =>
        Results.Ok(repository.Get(sellerId, from, to, cancellationToken));

    private static async Task DownloadReport(
        long sellerId,
        DateOnly from,
        DateOnly to,
        PostingExcelBuilder builder,
        HttpResponse response,
        CancellationToken token)
    {
        var fileName = $"postings_{sellerId}_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.xlsx";

        response.SetReportResponseHeaders(fileName);

        await builder.Build(
            sellerId,
            from,
            to,
            response.BodyWriter.AsStream(),
            token);
    }

    private static async Task DownloadCachedReport(
        long sellerId,
        DateOnly from,
        DateOnly to,
        CachedPostingReportGenerator reportGenerator,
        HttpResponse response,
        CancellationToken token)
    {
        var fileName = $"postings_{sellerId}_{from:yyyy-MM-dd}_{to:yyyy-MM-dd}.xlsx";

        response.SetReportResponseHeaders(fileName);

        await reportGenerator.GenerateCachedReport(
            new ReportRequest(
                sellerId,
                from,
                to),
            response.BodyWriter,
            token);
    }
}
