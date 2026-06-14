namespace OlapOverHttp.Host.ClickHouse.Infrastructure;

public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";

    public string ConnectionString { get; set; }
}
