using System.Data.Common;

namespace Common.Application.Data;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken ct = default);
}
