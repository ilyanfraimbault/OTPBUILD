using MySql.Data.MySqlClient;

namespace OTPBUILD.Configurations;

public class DatabaseConfig(string server, string uid, string pwd, string database)
{
    private readonly string _connectionString = $"Server={server};UserID={uid};Password={pwd};Database={database}";

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}