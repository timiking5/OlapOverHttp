using Octonica.ClickHouseClient;

namespace OlapOverHttp.Host.ClickHouse;

public sealed class ClickHouseConnectionFactory(ClickHouseConnectionSettings settings) : IClickHouseConnectionFactory
{
    public ClickHouseConnection CreateConnection() => new(settings);
}
