using Npgsql;
using OTPBUILD.Configurations;
using System.Data;

namespace OTPBUILD.Models;

public class DatabaseConnection(DatabaseConfig config) : IDisposable, IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource = config.CreateDataSource();
    private NpgsqlConnection? _connection;
    private bool _disposed;

    public async Task<NpgsqlConnection> GetOpenConnectionAsync()
    {
        if (_connection == null)
        {
            _connection = await _dataSource.OpenConnectionAsync();
        }
        else if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        return _connection;
    }

    public NpgsqlCommand CreateCommand(string commandText)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection must be opened before creating a command");
        }

        var command = _connection.CreateCommand();
        command.CommandText = commandText;
        command.CommandType = CommandType.Text;
        return command;
    }
    public NpgsqlCommand CreateStoredProcedure(string procedureName)
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Connection must be opened before creating a command");
        }

        var command = _connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        return command;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
                _dataSource.Dispose();
            }

            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
            }

            await _dataSource.DisposeAsync();

            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}