using Organisation.IntegrationLayer;
using System.Data;

namespace Organisation.SchedulingLayer
{
    public class OrgUnitStatements
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

        public static string InsertTasks
        {
            get
            {
                return INSERT_TASKS;
            }
        }

        public static string InsertItSystems
        {
            get
            {
                return INSERT_IT_SYSTEMS;
            }
        }

        public static string InsertContactForTasks
        {
            get
            {
                return INSERT_CONTACT_FOR_TASKS;
            }
        }

        public static string InsertContactPlaces
        {
            get
            {
                return INSERT_CONTACT_PLACES;
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
                switch (database)
                {
                    case DatabaseType.MSSQL:
                        return SELECT_SUCCESS_MSSQL;
                    case DatabaseType.MYSQL:
                        return SELECT_SUCCESS_MYSQL;
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

        public static string SelectSuccessTasks
        {
            get
            {
                return SELECT_SUCCESS_TASKS;
            }
        }

        public static string SelectItSystems
        {
            get
            {
                return SELECT_IT_SYSTEMS;
            }
        }

        public static string SelectSuccessItSystems
        {
            get
            {
                return SELECT_SUCCESS_IT_SYSTEMS;
            }
        }

        public static string SelectContactForTasks
        {
            get
            {
                return SELECT_CONTACT_FOR_TASKS;
            }
        }

        public static string SelectSuccessContactForTasks
        {
            get
            {
                return SELECT_SUCCESS_CONTACT_FOR_TASKS;
            }
        }

        public static string SelectContactPlaces
        {
            get
            {
                return SELECT_CONTACT_PLACES;
            }
        }

        public static string SelectSuccessContactPlaces
        {
            get
            {
                return SELECT_SUCCESS_CONTACT_PLACES;
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
                post_address_secondary,
                contact_open_hours,
                dtr_id,
                email_remarks,
                contact,
                post_return,
                phone_open_hours,
                bypass_cache,
                cvr,
                operation,
                foa,
                pnr,
                sor,
                priority
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
                @post_secondary,
                @contact_open_hours,
                @dtr_id,
                @email_remarks,
                @contact,
                @post_return,
                @phone_open_hours,
                @bypass_cache,
                @cvr,
                @operation,
                @foa,
                @pnr,
                @sor,
                @priority
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

        private const string INSERT_IT_SYSTEMS = @"
            INSERT INTO queue_orgunits_it_systems (
                unit_id,
                it_system_uuid
            )
            VALUES (
                @orgunit_id,
                @it_system_uuid
            )";

        private const string INSERT_CONTACT_PLACES = @"
            INSERT INTO queue_orgunits_contact_places (
                unit_id,
                contact_place_uuid
            )
            VALUES (
                @orgunit_id,
                @contact_place_uuid
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

        private static string SELECT_MSSQL
        {
            get
            {
                return @"SELECT TOP(" + rows + ") * FROM queue_orgunits ORDER BY priority, id";
            }
        }

        private static string SELECT_MYSQL
        {
            get
            {
                return @"SELECT * FROM queue_orgunits ORDER BY priority, id LIMIT " + rows;
            }
        }

        private const string SELECT_SUCCESS_MYSQL = @"SELECT * FROM success_orgunits WHERE uuid = @uuid ORDER BY id DESC LIMIT 1";
        private const string SELECT_SUCCESS_MSSQL = @"SELECT TOP(1) * FROM success_orgunits WHERE uuid = @uuid ORDER BY id DESC";

        private const string SELECT_TASKS = @"SELECT task FROM queue_orgunits_tasks WHERE unit_id = @id";
        private const string SELECT_SUCCESS_TASKS = @"SELECT task FROM success_orgunits_tasks WHERE unit_id = @id";
        private const string SELECT_IT_SYSTEMS = @"SELECT it_system_uuid FROM queue_orgunits_it_systems WHERE unit_id = @id";
        private const string SELECT_SUCCESS_IT_SYSTEMS = @"SELECT it_system_uuid FROM success_orgunits_it_systems WHERE unit_id = @id";
        private const string SELECT_CONTACT_FOR_TASKS = @"SELECT task FROM queue_orgunits_contact_for_tasks WHERE unit_id = @id";
        private const string SELECT_SUCCESS_CONTACT_FOR_TASKS = @"SELECT task FROM success_orgunits_contact_for_tasks WHERE unit_id = @id";
        private const string SELECT_CONTACT_PLACES = @"SELECT contact_place_uuid FROM queue_orgunits_contact_places WHERE unit_id = @id";
        private const string SELECT_SUCCESS_CONTACT_PLACES = @"SELECT contact_place_uuid FROM success_orgunits_contact_places WHERE unit_id = @id";

        private const string DELETE = @"DELETE FROM queue_orgunits WHERE id = @id";

        private const string INSERT_ON_SUCCESS = @"
            INSERT INTO success_orgunits (id, timestamp, uuid, shortkey, name, parent_ou_uuid, payout_ou_uuid, manager_uuid, orgunit_type, los_shortname, phone_number, email, location, ean, url, landline, post_address, post_address_secondary, contact_open_hours, email_remarks, contact, post_return, phone_open_hours, losid, dtr_id, foa, pnr, sor, cvr, operation, skipped) SELECT id, timestamp, uuid, shortkey, name, parent_ou_uuid, payout_ou_uuid, manager_uuid, orgunit_type, los_shortname, phone_number, email, location, ean, url, landline, post_address, post_address_secondary, contact_open_hours, email_remarks, contact, post_return, phone_open_hours, losid, dtr_id, foa, pnr, sor, cvr, operation, @skipped FROM queue_orgunits WHERE id = @id;
            INSERT INTO success_orgunits_contact_for_tasks (id, unit_id, task) SELECT id, unit_id, task FROM queue_orgunits_contact_for_tasks WHERE unit_id = @id;
            INSERT INTO success_orgunits_it_systems (id, unit_id, it_system_uuid) SELECT id, unit_id, it_system_uuid FROM queue_orgunits_it_systems WHERE unit_id = @id;
            INSERT INTO success_orgunits_tasks (id, unit_id, task) SELECT id, unit_id, task FROM queue_orgunits_tasks WHERE unit_id = @id;
            INSERT INTO success_orgunits_contact_places (id, unit_id, contact_place_uuid) SELECT id, unit_id, contact_place_uuid FROM queue_orgunits_contact_places WHERE unit_id = @id;";

        private const string INSERT_ON_FAILURE = @"
            INSERT INTO failure_orgunits (id, timestamp, uuid, shortkey, name, parent_ou_uuid, payout_ou_uuid, manager_uuid, orgunit_type, los_shortname, phone_number, email, location, ean, url, landline, post_address, post_address_secondary, contact_open_hours, email_remarks, contact, post_return, phone_open_hours, losid, dtr_id, foa, pnr, sor, cvr, operation, error) SELECT id, timestamp, uuid, shortkey, name, parent_ou_uuid, payout_ou_uuid, manager_uuid, orgunit_type, los_shortname, phone_number, email, location, ean, url, landline, post_address, post_address_secondary, contact_open_hours, email_remarks, contact, post_return, phone_open_hours, losid, dtr_id, foa, pnr, sor, cvr, operation, @error FROM queue_orgunits q WHERE id = @id;
            INSERT INTO failure_orgunits_contact_for_tasks (id, unit_id, task) SELECT id, unit_id, task FROM queue_orgunits_contact_for_tasks WHERE unit_id = @id;
            INSERT INTO failure_orgunits_it_systems (id, unit_id, it_system_uuid) SELECT id, unit_id, it_system_uuid FROM queue_orgunits_it_systems WHERE unit_id = @id;
            INSERT INTO failure_orgunits_tasks (id, unit_id, task) SELECT id, unit_id, task FROM queue_orgunits_tasks WHERE unit_id = @id;
            INSERT INTO failure_orgunits_contact_places (id, unit_id, contact_place_uuid) SELECT id, unit_id, contact_place_uuid FROM queue_orgunits_contact_places WHERE unit_id = @id;";

        private const string CLEANUP_MYSQL = @"
            DELETE FROM success_orgunits WHERE timestamp <= CURRENT_DATE() - INTERVAL 1 WEEK;
        ";

        private const string CLEANUP_MSSQL = @"
            DELETE FROM success_orgunits WHERE timestamp <= GETDATE() - 7;
        ";

    }
}
