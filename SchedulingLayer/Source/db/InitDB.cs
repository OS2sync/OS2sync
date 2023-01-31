using Organisation.IntegrationLayer;
using System;
using System.Data.Common;

namespace Organisation.SchedulingLayer
{
    public class InitDB
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void InitializeDatabase()
        {
            if (!string.IsNullOrEmpty(OrganisationRegistryProperties.GetInstance().DBConnectionString))
            {
                using (DbConnection connection = DaoUtil.GetConnection())
                {
                    try
                    {
                        string location = OrganisationRegistryProperties.GetInstance().MigrationScriptsPath;
                        log.Info($"Migrations location: {location}");

                        if (OrganisationRegistryProperties.GetInstance().Database.Equals(DatabaseType.MSSQL))
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
