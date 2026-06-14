namespace OlapOverHttp.Host.ClickHouse;

public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";

    public string ConnectionString { get; set; }
}
