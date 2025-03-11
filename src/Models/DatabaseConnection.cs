using MySql.Data.MySqlClient;
using OTPBUILD.Configurations;

namespace OTPBUILD.Models;

public class DatabaseConnection(DatabaseConfig databaseConfig)
{
    public MySqlConnection GetConnection()
    {
        return databaseConfig.GetConnection(); // Toujours créer une nouvelle connexion
    }
}
