using System.Diagnostics;

namespace OlapOverHttp.LoadTest.LoadTestCases;

public sealed class HotColdLoadTestCase(
    HttpClient client,
    List<long> sellerIds,
    DateOnly from
) : LoadTestCase
{
    public override async Task Request()
    {
        var sellerId = sellerIds[Random.Next(0, sellerIds.Count)];
        var startDate = GetRandomDate(from);
        var endPath = startDate.AddMonths(1);

        var path = $"/api/postings/hot-cold?sellerId={sellerId}&from={startDate:yyyy-MM-dd}&to={endPath:yyyy-MM-dd}";

        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(path);
            if (response.IsSuccessStatusCode)
                Interlocked.Increment(ref success);
            else
                Interlocked.Increment(ref failed);
        }
        catch
        {
            Interlocked.Increment(ref failed);
        }
        finally
        {
            Interlocked.Add(ref totalResponseTimeMs, sw.ElapsedMilliseconds);
            requestCount++;
        }
    }

    protected override DateOnly GetRandomDate(DateOnly from)
        => from.AddDays(Random.Next(0, 30));
}
