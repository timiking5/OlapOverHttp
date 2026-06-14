using System.Diagnostics;

namespace OlapOverHttp.LoadTest.LoadTestCases;

public sealed class ReportLoadTest(
    HttpClient client,
    List<long> sellerIds,
    DateOnly from
) : LoadTestCase
{
    public override async Task Request()
    {
        var sellerId = sellerIds[Random.Next(0, sellerIds.Count)];
        var startDate = GetRandomDate(from);
        var endDate = startDate.AddMonths(1);

        var path = $"/api/postings/report?sellerId={sellerId}&from={startDate:yyyy-MM-dd}&to={endDate:yyyy-MM-dd}";

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
}
