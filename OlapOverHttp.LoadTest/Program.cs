using OlapOverHttp.LoadTest;
using System.Diagnostics;

const double RequestsPerSecond = 250;
const int DurationSeconds = 30;

var from = new DateOnly(2026, 05, 13);
var sellerIds = LoadSellerIds("seller_ids.csv");

using var http = new HttpClient { BaseAddress = new Uri("https://localhost:7098") };

var loadTest = LoadTestFactory.GetLoadTestGenerator(
    LoadTestFactory.GetHotColdPostings,
    http,
    sellerIds,
    from);

var results = new List<Task>(capacity: (int)(DurationSeconds * RequestsPerSecond));
var interval = TimeSpan.FromSeconds(1) / RequestsPerSecond;
var end = Stopwatch.GetTimestamp() + Stopwatch.Frequency * DurationSeconds;
var next = Stopwatch.GetTimestamp();

while (Stopwatch.GetTimestamp() < end)
{
    results.Add(loadTest.Request()); // or await with concurrency control
    next += (long)(interval.TotalSeconds * Stopwatch.Frequency);
    var delay = (next - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency;
    if (delay > 0)
        await Task.Delay((int)Math.Ceiling(delay));
}

await Task.WhenAll(results);

loadTest.PrintResults(RequestsPerSecond, DurationSeconds, sellerIds.Count);

static List<long> LoadSellerIds(string path)
{
    var filePath = Path.Combine(AppContext.BaseDirectory, path);
    var sellerIds = new List<long>();

    foreach (var line in File.ReadLines(filePath).Skip(1))
    {
        if (!long.TryParse(line, out var sellerId))
            throw new FormatException($"Invalid seller id: {line}");

        sellerIds.Add(sellerId);
    }

    return sellerIds;
}
