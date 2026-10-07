using Microsoft.Data.SqlClient;

namespace Masroof.Infrastructure.Persistence;

/// <summary>Creates open SQL Server connections for the Dapper-based reporting and vector code.</summary>
public interface ISqlConnectionFactory
{
    Task<SqlConnection> OpenAsync(CancellationToken ct);
}

public sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
