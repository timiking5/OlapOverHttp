namespace OlapOverHttp.LoadTest.LoadTestCases;

public abstract class LoadTestCase
{
    protected static readonly Random Random = new();

    protected int success = 0;
    protected int failed = 0;
    protected long totalResponseTimeMs = 0;
    protected int requestCount = 0;

    public abstract Task Request();

    public virtual void PrintResults(int rps, int duration, int sellers)
    {
        Console.WriteLine($"Load test: {rps} rps for {duration}s ({sellers} sellers)");

        var meanResponseTimeMs = requestCount > 0 ? (double)totalResponseTimeMs / requestCount : 0;
        Console.WriteLine($"Done. Success: {success}, Failed: {failed}, Mean response time: {meanResponseTimeMs:F1} ms");
    }

    protected virtual DateOnly GetRandomDate(DateOnly from)
        => from.AddDays(Random.Next(0, 330));
}
