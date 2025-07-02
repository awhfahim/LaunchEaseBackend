using System.Data.Common;
using Common.Application.Data;
using Npgsql;

namespace Common.Infrastructure.Providers;

public class PgConnectionFactory: IDbConnectionFactory
{
    private readonly string _connectionString;

    public PgConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DbConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}