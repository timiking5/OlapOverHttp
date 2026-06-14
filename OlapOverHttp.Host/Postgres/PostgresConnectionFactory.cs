using Npgsql;

namespace OlapOverHttp.Host.Postgres;

public sealed class PostgresConnectionFactory(string connectionString) : IPostgresConnectionFactory
{
    public NpgsqlConnection CreateConnection() => new(connectionString);
}
