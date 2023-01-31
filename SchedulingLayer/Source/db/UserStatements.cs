using Organisation.IntegrationLayer;

namespace Organisation.SchedulingLayer
{
    public class UserStatements
    {
        public static string Insert
        {
            get
            {
                DatabaseType database = OrganisationRegistryProperties.GetInstance().Database;

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
                DatabaseType database = OrganisationRegistryProperties.GetInstance().Database;

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
                cvr,
                operation
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
                @cvr,
                @operation
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

        private const string SELECT_MYSQL = @"SELECT * FROM queue_users ORDER BY timestamp LIMIT 4";
        private const string SELECT_MSSQL = @"SELECT TOP(4) * FROM queue_users ORDER BY timestamp";

        private const string SELECT_SUCCESS = @"SELECT * FROM success_users WHERE uuid = @uuid";

        private const string SELECT_POSITIONS = @"SELECT name, orgunit_uuid, start_date, stop_date FROM queue_user_positions WHERE user_id = @id";

        private const string SELECT_SUCCESS_POSITIONS = @"SELECT name, orgunit_uuid, start_date, stop_date FROM success_user_positions WHERE user_id = @id";

        private const string DELETE = @"DELETE FROM queue_users WHERE id = @id";

        private const string INSERT_ON_SUCCESS = @"
            INSERT INTO success_users SELECT * FROM queue_users WHERE id = @id;
            INSERT INTO success_user_positions SELECT * FROM queue_user_positions WHERE user_id = @id;";

        private const string INSERT_ON_FAILURE = @"
            INSERT INTO failure_users SELECT q.*, @error FROM queue_users q WHERE id = @id;
            INSERT INTO failure_user_positions SELECT * FROM queue_user_positions WHERE user_id = @id;";
    }
}
