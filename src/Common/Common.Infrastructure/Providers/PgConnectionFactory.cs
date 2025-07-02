using System.Data.Common;
using Common.Application.Data;
using Common.Application.Options;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Common.Infrastructure.Providers;

public class PgConnectionFactory: IDbConnectionFactory
{
    private readonly ConnectionStringOptions _connectionString;

    public PgConnectionFactory(IOptions<ConnectionStringOptions> connectionString)
    {
        _connectionString = connectionString.Value;
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionString.Db);
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