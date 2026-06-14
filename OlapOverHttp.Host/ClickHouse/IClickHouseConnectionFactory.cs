using Octonica.ClickHouseClient;

namespace OlapOverHttp.Host.ClickHouse;

public interface IClickHouseConnectionFactory
{
    ClickHouseConnection CreateConnection();
}
