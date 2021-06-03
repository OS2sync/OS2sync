using Organisation.IntegrationLayer;

namespace Organisation.SchedulingLayer
{
    public class OrgUnitStatements
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

        public static string InsertTasks
        {
            get
            {
                return INSERT_TASKS;
            }
        }

        public static string InsertContactForTasks
        {
            get
            {
                return INSERT_CONTACT_FOR_TASKS;
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

        public static string SelectTasks
        {
            get
            {
                return SELECT_TASKS;
            }
        }

        public static string SelectContactForTasks
        {
            get
            {
                return SELECT_CONTACT_FOR_TASKS;
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
            INSERT INTO queue_orgunits (
                uuid,
                shortkey,
                name,
                parent_ou_uuid,
                payout_ou_uuid,
                manager_uuid,
                orgunit_type,
                los_shortname,
                losid,
                phone_number,
                email,
                location,
                ean,
                url,
                landline,
                post_address,
                contact_open_hours,
                dtr_id,
                email_remarks,
                contact,
                post_return,
                phone_open_hours,
                cvr,
                operation
            )
        ";

        private const string INSERT_SUFFIX = @"
            VALUES (
                @uuid,
                @shortkey,
                @name,
                @parent_ou_uuid,
                @payout_ou_uuid,
                @manager_uuid,
                @orgunit_type,
                @los_shortname,
                @los_id,
                @phone_number,
                @email,
                @location,
                @ean,
                @url,
                @landline,
                @post,
                @contact_open_hours,
                @dtr_id,
                @email_remarks,
                @contact,
                @post_return,
                @phone_open_hours,
                @cvr,
                @operation
            );
        ";

        private const string INSERT_MYSQL = INSERT_PREFIX + INSERT_SUFFIX + " SELECT LAST_INSERT_ID();";
        private const string INSERT_MSSQL = INSERT_PREFIX + " OUTPUT INSERTED.ID " + INSERT_SUFFIX;

        private const string INSERT_TASKS = @"
            INSERT INTO queue_orgunits_tasks (
                unit_id,
                task
            )
            VALUES (
                @orgunit_id,
                @task
            )";

        private const string INSERT_CONTACT_FOR_TASKS = @"
            INSERT INTO queue_orgunits_contact_for_tasks (
                unit_id,
                task
            )
            VALUES (
                @orgunit_id,
                @task
            )";

        private const string SELECT_MSSQL = @"SELECT TOP(4) * FROM queue_orgunits ORDER BY timestamp";
        private const string SELECT_MYSQL = @"SELECT * FROM queue_orgunits ORDER BY timestamp LIMIT 4";
        private const string SELECT_TASKS = @"SELECT task FROM queue_orgunits_tasks WHERE unit_id = @id";
        private const string SELECT_CONTACT_FOR_TASKS = @"SELECT task FROM queue_orgunits_contact_for_tasks WHERE unit_id = @id";

        private const string DELETE = @"DELETE FROM queue_orgunits WHERE id = @id";

        private const string INSERT_ON_SUCCESS = @"
            INSERT INTO success_orgunits SELECT * FROM queue_orgunits WHERE id = @id;
            INSERT INTO success_orgunits_contact_for_tasks SELECT * FROM queue_orgunits_contact_for_tasks WHERE unit_id = @id;
            INSERT INTO success_orgunits_tasks SELECT * FROM queue_orgunits_tasks WHERE unit_id = @id;";

        private const string INSERT_ON_FAILURE = @"
            INSERT INTO failure_orgunits SELECT q.*, @error FROM queue_orgunits q WHERE id = @id;
            INSERT INTO failure_orgunits_contact_for_tasks SELECT * FROM queue_orgunits_contact_for_tasks WHERE unit_id = @id;
            INSERT INTO failure_orgunits_tasks SELECT * FROM queue_orgunits_tasks WHERE unit_id = @id;";
    }
}
