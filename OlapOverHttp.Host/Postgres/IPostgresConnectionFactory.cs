using Npgsql;

namespace OlapOverHttp.Host.Postgres;

public interface IPostgresConnectionFactory
{
    NpgsqlConnection CreateConnection();
}
