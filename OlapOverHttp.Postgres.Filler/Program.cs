using OlapOverHttp.Postgres.Filler;
using Octonica.ClickHouseClient;

const int BatchSize = 10_000;
const int ParallelWeeks = 4;

var periodEnd = new DateTime(2026, 06, 13);
var periodStart = periodEnd.AddMonths(-2);

var clickHouseSettings = GetClickHouseConnectionSettings();
var postgresConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=posting";

var reader = new ClickHousePostingReader(clickHouseSettings);
await using var writer = new PostgresPostingWriter(postgresConnectionString);

Console.WriteLine(
    $"Syncing postings from ClickHouse to Postgres for period {periodStart:u} .. {periodEnd:u} " +
    $"(postgres batch size {BatchSize:N0})...");

var totalPostings = 0L;
var totalItems = 0L;
var batchNumber = 0;
var weekNumber = 0;

var weeks = EnumerateWeeks(periodStart, periodEnd).ToArray();

await Parallel.ForEachAsync(
    weeks.Select((range, index) => (Index: index + 1, range.Start, range.End)),
    new ParallelOptions { MaxDegreeOfParallelism = ParallelWeeks },
    async (week, cancellationToken) =>
    {
        await using var writer = new PostgresPostingWriter(postgresConnectionString);
        var weekPostings = 0L;
        var weekItems = 0L;
        Console.WriteLine($"Week {week.Index}: {week.Start:u} .. {week.End:u}");
        await foreach (var batch in reader
            .ReadBatchAsync(week.Start, week.End, cancellationToken)
            .Chunk(BatchSize))
        {
            var (postings, items) = await writer.SyncBatch(batch);
            Interlocked.Increment(ref batchNumber);
            Interlocked.Add(ref totalPostings, postings);
            Interlocked.Add(ref totalItems, items);
            weekPostings += postings;
            weekItems += items;
            Console.WriteLine(
                $"  Week {week.Index}, batch synced: {postings:N0} postings, {items:N0} items.");
        }
        Console.WriteLine(
            $"Week {week.Index} finished: {weekPostings:N0} postings, {weekItems:N0} items.");
    });

Console.WriteLine(
    $"Done. Synced {totalPostings:N0} postings and {totalItems:N0} items in {batchNumber} batches across {weekNumber} weeks.");

static ClickHouseConnectionSettings GetClickHouseConnectionSettings()
{
    return new ClickHouseConnectionStringBuilder
    {
        Host = "localhost",
        Port = 9000,
        User = "clickhouse",
        Password = "clickhouse",
        Database = "posting",
        ReadWriteTimeout = 3600 * 1000,
        CommandTimeout = 3600 * 1000
    }.BuildSettings();
}

static IEnumerable<(DateTime Start, DateTime End)> EnumerateWeeks(
    DateTime periodStart,
    DateTime periodEnd)
{
    var weekStart = periodStart;
    while (weekStart < periodEnd)
    {
        var weekEnd = weekStart.AddDays(7);
        if (weekEnd > periodEnd)
            weekEnd = periodEnd;
        yield return (weekStart, weekEnd);
        weekStart = weekEnd;
    }
}
