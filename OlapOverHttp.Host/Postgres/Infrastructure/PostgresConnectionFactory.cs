using Npgsql;

namespace OlapOverHttp.Host.Postgres.Infrastructure;

public sealed class PostgresConnectionFactory(string connectionString) : IPostgresConnectionFactory
{
    public NpgsqlConnection CreateConnection() => new(connectionString);
}
