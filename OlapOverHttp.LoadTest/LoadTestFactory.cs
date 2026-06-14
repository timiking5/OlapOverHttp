using OlapOverHttp.LoadTest.LoadTestCases;

namespace OlapOverHttp.LoadTest;

public static class LoadTestFactory
{
    public const string GetPostings = "GetPostings";

    public const string GetReports = "GetReports";

    public const string GetCachedReports = "GetCachedReports";

    public const string GetHotColdPostings = "GetHotColdPostings";

    public static LoadTestCase GetLoadTestGenerator(
        string loadTestCase,
        HttpClient client,
        List<long> sellerIds,
        DateOnly from)
    {
        return loadTestCase switch
        {
            GetPostings => new PostingLoadTest(client, sellerIds, from),
            GetHotColdPostings => new HotColdLoadTestCase(client, sellerIds, from),
            GetReports => new ReportLoadTest(client, sellerIds, from),
            GetCachedReports => new CachedReportLoadTestCase(client, sellerIds, from),
            _ => throw new ArgumentOutOfRangeException(nameof(loadTestCase)),
        };
    }
}
