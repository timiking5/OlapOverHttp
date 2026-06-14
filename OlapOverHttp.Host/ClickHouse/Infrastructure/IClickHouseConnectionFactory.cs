using Octonica.ClickHouseClient;

namespace OlapOverHttp.Host.ClickHouse.Infrastructure;

public interface IClickHouseConnectionFactory
{
    ClickHouseConnection CreateConnection();
}
