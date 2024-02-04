using Organisation.IntegrationLayer;
using System;

namespace Organisation.SchedulingLayer
{
    public class UserStatements
    {
        private static DatabaseType database = "MYSQL".Equals(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DBType) ? DatabaseType.MYSQL : DatabaseType.MSSQL;
        private static string rows = OrganisationRegistryProperties.AppSettings.SchedulerSettings.Threads.ToString();
        
        public static string Insert
        {
            get
            {
                switch (database)
                {
                    case DatabaseType.MSSQL:
                        return INSERT_MSSQL;
                    case DatabaseType.MYSQL:
                        return INSERT_MYSQL;
                    default:
                        throw new System.Exception("Unknown database type: " + database);
                }
            }
        }

        public static string InsertPositions
        {
            get
            {
                return INSERT_POSITION;
            }
        }

        public static string Select
        {
            get
            {
                switch (database)
                {
                    case DatabaseType.MSSQL:
                        return SELECT_MSSQL;
                    case DatabaseType.MYSQL:
                        return SELECT_MYSQL;
                    default:
                        throw new System.Exception("Unknown database type: " + database);
                }
            }
        }

        public static string SelectSuccess
        {
            get
            {
                return SELECT_SUCCESS;
            }
        }

        public static string SelectPositions
        {
            get
            {
                return SELECT_POSITIONS;
            }
        }

        public static string SelectSuccessPositions
        {
            get
            {
                return SELECT_SUCCESS_POSITIONS;
            }
        }

        public static string Delete
        {
            get
            {
                return DELETE;
            }
        }

        public static string Success
        {
            get
            {
                return INSERT_ON_SUCCESS;
            }
        }

        public static string Failure
        {
            get
            {
                return INSERT_ON_FAILURE;
            }
        }

        public static string Cleanup
        {
            get
            {
                switch (database)
                {
                    case DatabaseType.MSSQL:
                        return CLEANUP_MSSQL;
                    case DatabaseType.MYSQL:
                        return CLEANUP_MYSQL;
                    default:
                        throw new System.Exception("Unknown database type: " + database);
                }
            }
        }

        private const string INSERT_PREFIX = @"
            INSERT INTO queue_users (
                uuid,
                shortkey,
                user_id,
                phone_number,
                email,
                racfid,
                location,
                fmk_id,
                landline,
                name,
                cpr,
                bypass_cache,
                cvr,
                operation,
                priority
            )";

        private const string INSERT_SUFFIX = @"
            VALUES (
                @uuid,
                @shortkey,
                @user_id,
                @phone_number,
                @email,
                @racfid,
                @location,
                @fmk_id,
                @landline,
                @name,
                @cpr,
                @bypass_cache,
                @cvr,
                @operation,
                @priority
            );
        ";

        private const string INSERT_MYSQL = INSERT_PREFIX + INSERT_SUFFIX + " SELECT LAST_INSERT_ID();";
        private const string INSERT_MSSQL = INSERT_PREFIX + " OUTPUT INSERTED.ID " + INSERT_SUFFIX;

        private const string INSERT_POSITION = @"
            INSERT INTO queue_user_positions (
                user_id,
                name,
                orgunit_uuid,
                start_date,
                stop_date
            )
            VALUES (
                @user_id,
                @name,
                @orgunit_uuid,
                @start_date,
                @stop_date
            )";

        private static string SELECT_MYSQL
        {
            get
            {
                return @"SELECT * FROM queue_users ORDER BY priority, id LIMIT " + rows;
            }
        }

        private static string SELECT_MSSQL
        {
            get
            {
                return @"SELECT TOP(" + rows + ") * FROM queue_users ORDER BY priority, id";
            }
        }

        private const string SELECT_SUCCESS = @"SELECT * FROM success_users WHERE uuid = @uuid";

        private const string SELECT_POSITIONS = @"SELECT name, orgunit_uuid, start_date, stop_date FROM queue_user_positions WHERE user_id = @id";

        private const string SELECT_SUCCESS_POSITIONS = @"SELECT name, orgunit_uuid, start_date, stop_date FROM success_user_positions WHERE user_id = @id";

        private const string DELETE = @"DELETE FROM queue_users WHERE id = @id";

        private const string INSERT_ON_SUCCESS = @"
            INSERT INTO success_users (id, timestamp, uuid, shortkey, user_id, phone_number, email, location, name, cpr, cvr, operation, racfid, landline, fmk_id, skipped) SELECT id, timestamp, uuid, shortkey, user_id, phone_number, email, location, name, cpr, cvr, operation, racfid, landline, fmk_id, @skipped FROM queue_users WHERE id = @id;
            INSERT INTO success_user_positions (id, user_id, name, orgunit_uuid, start_date, stop_date) SELECT id, user_id, name, orgunit_uuid, start_date, stop_date FROM queue_user_positions WHERE user_id = @id;";

        private const string INSERT_ON_FAILURE = @"
            INSERT INTO failure_users (id, timestamp, uuid, shortkey, user_id, phone_number, email, location, name, cpr, cvr, operation, racfid, landline, fmk_id, error) SELECT id, timestamp, uuid, shortkey, user_id, phone_number, email, location, name, cpr, cvr, operation, racfid, landline, fmk_id, @error FROM queue_users q WHERE id = @id;
            INSERT INTO failure_user_positions (id, user_id, name, orgunit_uuid, start_date, stop_date) SELECT id, user_id, name, orgunit_uuid, start_date, stop_date FROM queue_user_positions WHERE user_id = @id;";

        private const string CLEANUP_MYSQL = @"
            DELETE FROM success_users WHERE timestamp <= CURRENT_DATE() - INTERVAL 1 WEEK;
        ";

        private const string CLEANUP_MSSQL = @"
            DELETE FROM success_users WHERE timestamp <= GETDATE() - 7;
        ";
    }
}
