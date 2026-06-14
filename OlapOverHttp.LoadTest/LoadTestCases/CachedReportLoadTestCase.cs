using System.Diagnostics;

namespace OlapOverHttp.LoadTest.LoadTestCases;

public sealed class CachedReportLoadTestCase(
    HttpClient client,
    List<long> sellerIds,
    DateOnly from
) : LoadTestCase
{
    private readonly DateOnly _actualFrom = new(from.Year, from.Month, 1);
    private int _sellerIndex = 0;

    public override async Task Request()
    {
        var sellerId = sellerIds[_sellerIndex % sellerIds.Count];
        Interlocked.Add(ref _sellerIndex, 1);

        var startDate = _actualFrom.AddMonths(1);
        var endDate = startDate.AddMonths(1);

        var path = $"/api/postings/cached-report?sellerId={sellerId}&from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}";

        var sw = Stopwatch.StartNew();
        try
        {
            using var response = await client.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(Stream.Null);
                Interlocked.Increment(ref success);
            }
            else
            {
                Interlocked.Increment(ref failed);
            }
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

    protected override DateOnly GetRandomDate(DateOnly from) => from.AddMonths(Random.Next(0, 12));
}
