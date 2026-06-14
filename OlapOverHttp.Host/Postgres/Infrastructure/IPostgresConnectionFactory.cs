using Npgsql;

namespace OlapOverHttp.Host.Postgres.Infrastructure;

public interface IPostgresConnectionFactory
{
    NpgsqlConnection CreateConnection();
}
