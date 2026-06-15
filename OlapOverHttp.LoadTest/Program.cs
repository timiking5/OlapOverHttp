using OlapOverHttp.LoadTest;
using System.Diagnostics;

var requestsPerSecond = 10d;
var durationSeconds = 30;
var from = new DateOnly(2026, 05, 13);
var loadTestCase = LoadTestFactory.GetHotColdPostings;

for (var i = 0; i < args.Length; i++)
    switch (args[i])
    {
        case "--requests-per-second":
            requestsPerSecond = double.Parse(args[++i]);
            break;
        case "--duration-seconds":
            durationSeconds = int.Parse(args[++i]);
            break;
        case "--from":
            from = DateOnly.Parse(args[++i]);
            break;
        case "--case":
            loadTestCase = args[++i];
            break;
    }

var sellerIds = LoadSellerIds("seller_ids.csv");

using var http = new HttpClient { BaseAddress = new Uri("https://localhost:7098") };

var loadTest = LoadTestFactory.GetLoadTestGenerator(loadTestCase, http, sellerIds, from);

var results = new List<Task>(capacity: (int)(durationSeconds * requestsPerSecond));
var interval = TimeSpan.FromSeconds(1) / requestsPerSecond;
var end = Stopwatch.GetTimestamp() + Stopwatch.Frequency * durationSeconds;
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

loadTest.PrintResults(requestsPerSecond, durationSeconds, sellerIds.Count);

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
