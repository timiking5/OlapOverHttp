using OlapOverHttp.LoadTest;

const int RequestsPerSecond = 20;
const int DurationSeconds = 30;

var from = new DateOnly(2025, 06, 13);  // min date in clickhouse
var sellerIds = LoadSellerIds("seller_ids.csv");

using var http = new HttpClient { BaseAddress = new Uri("https://localhost:7098") };

var loadTest = LoadTestFactory.GetLoadTestGenerator(
    LoadTestFactory.GetCachedReports,
    http,
    sellerIds,
    from);

using var timer = new Timer(async _ => await loadTest.Request(), null, 0, 1000 / RequestsPerSecond);

await Task.Delay(DurationSeconds * 1000);
timer.Dispose();

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
