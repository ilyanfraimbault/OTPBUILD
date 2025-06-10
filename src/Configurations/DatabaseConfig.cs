using Npgsql;

namespace OTPBUILD.Configurations;

public class DatabaseConfig(string server, string uid, string pwd, string database)
{
    private readonly string _connectionString = $"Host={server};UserID={uid};Password={pwd};Database={database};";

    public NpgsqlDataSource CreateDataSource()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
        return dataSourceBuilder.Build();
    }
}