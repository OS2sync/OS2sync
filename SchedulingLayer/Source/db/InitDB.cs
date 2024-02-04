using Organisation.IntegrationLayer;
using System;
using System.Data.Common;

namespace Organisation.SchedulingLayer
{
    public class InitDB
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static DatabaseType database = "MYSQL".Equals(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBType) ? DatabaseType.MYSQL : DatabaseType.MSSQL;
        private static string connectionString = OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBConnectionString;

        public static void InitializeDatabase()
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                using (DbConnection connection = DaoUtil.GetConnection())
                {
                    try
                    {
                        string location = OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBMigrationPath;
                        log.Info($"Migrations location: {location}");

                        if (database.Equals(DatabaseType.MSSQL))
                        {
                            var evolve = new Evolve.Evolve(connection, msg => log.Info(msg))
                            {
                                Locations = new[] { location },
                                Schemas = new[] { "dbo" }, // default schema can be NULL in SQL Server, which makes Evolve unhappy
                                IsEraseDisabled = true
                            };

                            evolve.Migrate();
                        }
                        else
                        {
                            var evolve = new Evolve.Evolve(connection, msg => log.Info(msg))
                            {
                                Locations = new[] { location },
                                IsEraseDisabled = true
                            };

                            evolve.Migrate();
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
