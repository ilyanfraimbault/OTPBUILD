using MySql.Data.MySqlClient;
using OTPBUILD.Configurations;

namespace OTPBUILD.Models;

public class DatabaseConnection(DatabaseConfig databaseConfig)
{
    private static MySqlConnection? _connection;

    public MySqlConnection GetConnection()
    {
        return _connection ??= databaseConfig.GetConnection();
    }
}