using OlapOverHttp.Clickhouse.Filler;
using Octonica.ClickHouseClient;
using SharpJuice.Clickhouse;

const int PostingsPerMonth = 2_000_000;
const int MonthsInPeriod = 12;
const int TotalPostings = PostingsPerMonth * MonthsInPeriod;
const int InsertBatchSize = 10_000;
const int ParallelWorkers = 10;
const string ConnectionStringEnvVar = "CLICKHOUSE_CONNECTION_STRING";
const string SellerIdsFile = "seller_ids.csv";

var connectionFactory = new ClickHouseConnectionFactory(GetConnectionSettings());

var sellerIds = LoadSellerIds(SellerIdsFile);

Console.WriteLine(
    $"Loaded {sellerIds.Count} sellers. Generating {TotalPostings:N0} postings " +
    $"({PostingsPerMonth:N0}/month × {MonthsInPeriod} months)...");

var postingsWriter = new TableWriterBuilder(connectionFactory)
    .For<PostingRow>("postings")
    .AddColumn("posting_id", x => x.PostingId)
    .AddColumn("posting_name", x => x.PostingName)
    .AddColumn("seller_id", x => x.SellerId)
    .AddColumn("item_ids", x => x.ItemIds)
    .AddColumn("item_quantities", x => x.ItemQuantities)
    .AddColumn("marketplace_item_prices", x => x.MarketplaceItemPrices)
    .AddColumn("seller_item_prices", x => x.SellerItemPrices)
    .AddColumn("seller_currency", x => x.SellerCurrency)
    .AddColumn("seller_fx_rate", x => x.SellerFxRate)
    .AddColumn("posting_created_at", x => x.PostingCreatedAt)
    .AddColumn("posting_delivered_at", x => x.PostingDeliveredAt)
    .AddColumn("posting_source", x => x.PostingSource)
    .AddColumn("total_amount", x => x.TotalAmount)
    .AddColumn("payment_method", x => x.PaymentMethod)
    .AddColumn("shipping_country", x => x.ShippingCountry)
    .AddColumn("shipping_city", x => x.ShippingCity)
    .AddColumn("version", x => x.Version)
    .Build();

var periodEnd = DateTime.UtcNow;
var periodStart = periodEnd.AddYears(-1);

var insertedPostingRows = 0L;
var completedMonths = 0;

await Parallel.ForEachAsync(
    Enumerable.Range(0, MonthsInPeriod),
    new ParallelOptions { MaxDegreeOfParallelism = ParallelWorkers },
    async (monthIndex, cancellationToken) =>
    {
        var firstPostingIndex = (long)monthIndex * PostingsPerMonth;
        var lastPostingIndex = firstPostingIndex + PostingsPerMonth;
        var postingBatch = new List<PostingRow>(InsertBatchSize);

        for (var postingIndex = firstPostingIndex; postingIndex < lastPostingIndex; postingIndex++)
        {
            var postingId = postingIndex + 1;
            postingBatch.Add(PostingGenerator.Generate(
                postingId,
                sellerIds,
                periodStart,
                periodEnd,
                monthIndex,
                MonthsInPeriod));

            if (postingBatch.Count >= InsertBatchSize)
            {
                await postingsWriter.Insert(postingBatch, cancellationToken);
                Interlocked.Add(ref insertedPostingRows, postingBatch.Count);
                postingBatch.Clear();
            }
        }

        if (postingBatch.Count > 0)
        {
            await postingsWriter.Insert(postingBatch, cancellationToken);
            Interlocked.Add(ref insertedPostingRows, postingBatch.Count);
        }

        var completed = Interlocked.Increment(ref completedMonths);
        Console.WriteLine($"Month {completed}/{MonthsInPeriod} finished.");
    });

Console.WriteLine(
    $"Done. Inserted {insertedPostingRows:N0} posting rows for {TotalPostings:N0} postings.");

static ClickHouseConnectionSettings GetConnectionSettings()
{
    var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvVar);

    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        return new ClickHouseConnectionStringBuilder(connectionString).BuildSettings();
    }

    return new ClickHouseConnectionStringBuilder
    {
        Host = "localhost",
        Port = 9000,
        User = "clickhouse",
        Password = "clickhouse",
        Database = "posting"
    }.BuildSettings();
}

static List<long> LoadSellerIds(string path)
{
    var filePath = Path.Combine(AppContext.BaseDirectory, path);
    var sellerIds = new List<long>(5_000);

    foreach (var line in File.ReadLines(filePath).Skip(1))
    {
        if (!long.TryParse(line, out var sellerId))
            throw new FormatException($"Invalid seller id: {line}");

        sellerIds.Add(sellerId);
    }

    return sellerIds;
}
