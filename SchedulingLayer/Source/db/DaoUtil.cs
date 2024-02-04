using Organisation.IntegrationLayer;
using System.Data.Common;
using System.Data.SqlClient;
using MySqlConnector;

namespace Organisation.SchedulingLayer
{
    public class DaoUtil
    {
        private static DatabaseType database = "MYSQL".Equals(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBType) ? DatabaseType.MYSQL : DatabaseType.MSSQL;
        private static string connectionString = OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBConnectionString;

        public static DbConnection GetConnection()
        {
            switch (database)
            {
                case DatabaseType.MSSQL:
                    return new SqlConnection(connectionString);
                case DatabaseType.MYSQL:
                    return new MySqlConnection(connectionString);
                default:
                    throw new System.Exception("Unknown database type: " + database);
            }
        }

        public static DbCommand GetCommand(string statement, DbConnection connection)
        {
            switch (database)
            {
                case DatabaseType.MSSQL:
                    return new SqlCommand(statement, (SqlConnection) connection);
                case DatabaseType.MYSQL:
                    return new MySqlCommand(statement, (MySqlConnection) connection);
                default:
                    throw new System.Exception("Unknown database type: " + database);
            }
        }

        public static object GetParameter(string key, object value)
        {
            switch (database)
            {
                case DatabaseType.MSSQL:
                    return new SqlParameter(key, value);
                case DatabaseType.MYSQL:
                    return new MySqlParameter(key, value);
                default:
                    throw new System.Exception("Unknown database type: " + database);
            }
        }
    }
}
