using Octonica.ClickHouseClient;

namespace OlapOverHttp.Host.ClickHouse.Infrastructure;

public sealed class ClickHouseConnectionFactory(ClickHouseConnectionSettings settings) : IClickHouseConnectionFactory
{
    public ClickHouseConnection CreateConnection() => new(settings);
}
