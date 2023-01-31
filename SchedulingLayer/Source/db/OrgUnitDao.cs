using Organisation.BusinessLayer.DTO.Registration;
using Organisation.IntegrationLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using static Organisation.BusinessLayer.DTO.Registration.OrgUnitRegistration;

namespace Organisation.SchedulingLayer
{
    public class OrgUnitDao
    {
        private string connectionString = null;

        public OrgUnitDao()
        {
            connectionString = OrganisationRegistryProperties.GetInstance().DBConnectionString;
        }

        public void Save(OrgUnitRegistration ou, OperationType operation, string cvr)
        {
            long orgunit_id = 0;

            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.Insert, connection))
                        {
                            command.Transaction = transaction;

                            command.Parameters.Add(DaoUtil.GetParameter("@uuid", ou.Uuid));
                            command.Parameters.Add(DaoUtil.GetParameter("@shortkey", (object)ou.ShortKey ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@name", (object)ou.Name ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@parent_ou_uuid", (object)ou.ParentOrgUnitUuid ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@payout_ou_uuid", (object)ou.PayoutUnitUuid ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@cvr", cvr));
                            command.Parameters.Add(DaoUtil.GetParameter("@operation", operation.ToString()));

                            command.Parameters.Add(DaoUtil.GetParameter("@orgunit_type", ou.Type.ToString()));
                            command.Parameters.Add(DaoUtil.GetParameter("@los_shortname", (object)ou.LOSShortName ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@los_id", (object)ou.LOSId ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@phone_number", (object)ou.PhoneNumber ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@url", (object)ou.Url ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@landline", (object)ou.Landline ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@email", (object)ou.Email ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@location", (object)ou.Location ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@ean", (object)ou.Ean ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@contact_open_hours", (object)ou.ContactOpenHours ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@dtr_id", (object)ou.DtrId ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@email_remarks", (object)ou.EmailRemarks ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@contact", (object)ou.Contact ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@post_return", (object)ou.PostReturn ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@phone_open_hours", (object)ou.PhoneOpenHours ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@post", (object)ou.Post ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@foa", (object)ou.FOA ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@pnr", (object)ou.PNR ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@sor", (object)ou.SOR ?? DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@manager_uuid", (object)ou.ManagerUuid ?? DBNull.Value));

                            orgunit_id = Convert.ToInt64(command.ExecuteScalar());
                        }

                        // insert itSystems
                        foreach (string itSystem in ou.ItSystems ?? Enumerable.Empty<string>())
                        {
                            using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.InsertItSystems, connection))
                            {
                                command.Transaction = transaction;

                                command.Parameters.Add(DaoUtil.GetParameter("@orgunit_id", orgunit_id));
                                command.Parameters.Add(DaoUtil.GetParameter("@it_system_uuid", itSystem));

                                command.ExecuteNonQuery();
                            }
                        }

                        // insert tasks
                        foreach (string task in ou.Tasks ?? Enumerable.Empty<string>())
                        {
                            using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.InsertTasks, connection))
                            {
                                command.Transaction = transaction;

                                command.Parameters.Add(DaoUtil.GetParameter("@orgunit_id", orgunit_id));
                                command.Parameters.Add(DaoUtil.GetParameter("@task", task));

                                command.ExecuteNonQuery();
                            }
                        }

                        // insert contact for tasks
                        foreach (string task in ou.ContactForTasks ?? Enumerable.Empty<string>())
                        {
                            using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.InsertContactForTasks, connection))
                            {
                                command.Transaction = transaction;

                                command.Parameters.Add(DaoUtil.GetParameter("@orgunit_id", orgunit_id));
                                command.Parameters.Add(DaoUtil.GetParameter("@task", task));

                                command.ExecuteNonQuery();
                            }
                        }

                        // insert contactplaces
                        foreach (string contactPlace in ou.ContactPlaces ?? Enumerable.Empty<string>())
                        {
                            using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.InsertContactPlaces, connection))
                            {
                                command.Transaction = transaction;

                                command.Parameters.Add(DaoUtil.GetParameter("@orgunit_id", orgunit_id));
                                command.Parameters.Add(DaoUtil.GetParameter("@contact_place_uuid", contactPlace));

                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();

                        throw;
                    }
                }
            }

        }

        public List<OrgUnitRegistrationExtended> Get4OldestEntries()
        {
            List<OrgUnitRegistrationExtended> result = new List<OrgUnitRegistrationExtended>();

            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.Select, connection))
                {
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long orgUnitId = (long)reader["id"];

                            var orgUnit = new OrgUnitRegistrationExtended();
                            orgUnit.Id = orgUnitId;
                            orgUnit.Uuid = GetValue(reader, "uuid");
                            orgUnit.ShortKey = GetValue(reader, "shortkey");
                            orgUnit.Name = GetValue(reader, "name");
                            orgUnit.ParentOrgUnitUuid = GetValue(reader, "parent_ou_uuid");
                            orgUnit.PayoutUnitUuid = GetValue(reader, "payout_ou_uuid");
                            orgUnit.ManagerUuid = GetValue(reader, "manager_uuid");
                            orgUnit.LOSShortName = GetValue(reader, "los_shortname");
                            orgUnit.LOSId = GetValue(reader, "losid");
                            orgUnit.PhoneNumber = GetValue(reader, "phone_number");
                            orgUnit.Email = GetValue(reader, "email");
                            orgUnit.Landline = GetValue(reader, "landline");
                            orgUnit.Url = GetValue(reader, "url");
                            orgUnit.Location = GetValue(reader, "location");
                            orgUnit.Ean = GetValue(reader, "ean");
                            orgUnit.Post = GetValue(reader, "post_address");
                            orgUnit.ContactOpenHours = GetValue(reader, "contact_open_hours");
                            orgUnit.DtrId = GetValue(reader, "dtr_id");
                            orgUnit.EmailRemarks = GetValue(reader, "email_remarks");
                            orgUnit.Contact = GetValue(reader, "contact");
                            orgUnit.PostReturn = GetValue(reader, "post_return");
                            orgUnit.PhoneOpenHours = GetValue(reader, "phone_open_hours");
                            orgUnit.FOA = GetValue(reader, "foa");
                            orgUnit.PNR = GetValue(reader, "pnr");
                            orgUnit.SOR = GetValue(reader, "sor");
                            orgUnit.Operation = (OperationType)Enum.Parse(typeof(OperationType), GetValue(reader, "operation"));
                            orgUnit.Cvr = GetValue(reader, "cvr");

                            // type can be null on DELETEs                        
                            string tmpType = GetValue(reader, "orgunit_type");
                            if (!string.IsNullOrEmpty(tmpType)) {
                                orgUnit.Type = (OrgUnitType)Enum.Parse(typeof(OrgUnitType), tmpType);    
                            }
                            else {
                                orgUnit.Type = OrgUnitType.DEPARTMENT;
                            }

                            result.Add(orgUnit);
                        }
                    }
                }

                foreach (var orgUnit in result)
                {
                    orgUnit.Tasks = new List<string>();
                    orgUnit.ContactForTasks = new List<string>();
                    orgUnit.ItSystems = new List<string>();
                    orgUnit.ContactPlaces = new List<string>();

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectTasks, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string task = GetValue(reader, "task");

                                orgUnit.Tasks.Add(task);
                            }
                        }
                    }

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectItSystems, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string itSystemUuid = GetValue(reader, "it_system_uuid");

                                orgUnit.ItSystems.Add(itSystemUuid);
                            }
                        }
                    }

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectContactForTasks, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string task = GetValue(reader, "task");

                                orgUnit.ContactForTasks.Add(task);
                            }
                        }
                    }

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectContactPlaces, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string contactPlace = GetValue(reader, "contact_place_uuid");

                                orgUnit.ContactPlaces.Add(contactPlace);
                            }
                        }
                    }

                }
            }

            return result;
        }

        public List<OrgUnitRegistrationExtended> GetSuccessEntries(String uuid)
        {
            List<OrgUnitRegistrationExtended> result = new List<OrgUnitRegistrationExtended>();

            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectSuccess, connection))
                {
                    command.Parameters.Add(DaoUtil.GetParameter("@uuid", uuid));

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            long orgUnitId = (long)reader["id"];

                            var orgUnit = new OrgUnitRegistrationExtended();
                            orgUnit.Id = orgUnitId;
                            orgUnit.Uuid = GetValue(reader, "uuid");
                            orgUnit.ShortKey = GetValue(reader, "shortkey");
                            orgUnit.Name = GetValue(reader, "name");
                            orgUnit.ParentOrgUnitUuid = GetValue(reader, "parent_ou_uuid");
                            orgUnit.PayoutUnitUuid = GetValue(reader, "payout_ou_uuid");
                            orgUnit.ManagerUuid = GetValue(reader, "manager_uuid");
                            orgUnit.LOSShortName = GetValue(reader, "los_shortname");
                            orgUnit.LOSId = GetValue(reader, "losid");
                            orgUnit.PhoneNumber = GetValue(reader, "phone_number");
                            orgUnit.Email = GetValue(reader, "email");
                            orgUnit.Landline = GetValue(reader, "landline");
                            orgUnit.Url = GetValue(reader, "url");
                            orgUnit.Location = GetValue(reader, "location");
                            orgUnit.Ean = GetValue(reader, "ean");
                            orgUnit.Post = GetValue(reader, "post_address");
                            orgUnit.ContactOpenHours = GetValue(reader, "contact_open_hours");
                            orgUnit.DtrId = GetValue(reader, "dtr_id");
                            orgUnit.EmailRemarks = GetValue(reader, "email_remarks");
                            orgUnit.Contact = GetValue(reader, "contact");
                            orgUnit.PostReturn = GetValue(reader, "post_return");
                            orgUnit.PhoneOpenHours = GetValue(reader, "phone_open_hours");
                            orgUnit.FOA = GetValue(reader, "foa");
                            orgUnit.PNR = GetValue(reader, "pnr");
                            orgUnit.SOR = GetValue(reader, "sor");
                            orgUnit.Operation = (OperationType)Enum.Parse(typeof(OperationType), GetValue(reader, "operation"));
                            orgUnit.Cvr = GetValue(reader, "cvr");

                            // type can be null on DELETEs                        
                            string tmpType = GetValue(reader, "orgunit_type");
                            if (!string.IsNullOrEmpty(tmpType))
                            {
                                orgUnit.Type = (OrgUnitType)Enum.Parse(typeof(OrgUnitType), tmpType);
                            }
                            else
                            {
                                orgUnit.Type = OrgUnitType.DEPARTMENT;
                            }

                            result.Add(orgUnit);
                        }
                    }
                }

                foreach (var orgUnit in result)
                {
                    orgUnit.Tasks = new List<string>();
                    orgUnit.ContactForTasks = new List<string>();
                    orgUnit.ItSystems = new List<string>();
                    orgUnit.ContactPlaces = new List<string>();

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectSuccessTasks, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string task = GetValue(reader, "task");

                                orgUnit.Tasks.Add(task);
                            }
                        }
                    }

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectSuccessItSystems, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string itSystemUuid = GetValue(reader, "it_system_uuid");

                                orgUnit.ItSystems.Add(itSystemUuid);
                            }
                        }
                    }

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectSuccessContactForTasks, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string task = GetValue(reader, "task");

                                orgUnit.ContactForTasks.Add(task);
                            }
                        }
                    }

                    using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.SelectSuccessContactPlaces, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", orgUnit.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string contactPlace = GetValue(reader, "contact_place_uuid");

                                orgUnit.ContactPlaces.Add(contactPlace);
                            }
                        }
                    }

                }
            }

            return result;
        }

        public void Delete(long id)
        {
            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.Delete, connection))
                {
                    command.Parameters.Add(DaoUtil.GetParameter("@id", id));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void OnSuccess(long id)
        {
            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.Success, connection))
                {
                    command.Parameters.Add(DaoUtil.GetParameter("@id", id));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void OnFailure(long id, string errorMessage)
        {
            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(OrgUnitStatements.Failure, connection))
                {
                    command.Parameters.Add(DaoUtil.GetParameter("@id", id));
                    command.Parameters.Add(DaoUtil.GetParameter("@error", errorMessage));
                    command.ExecuteNonQuery();
                }
            }
        }

        private string GetValue(DbDataReader reader, string key)
        {
            if (reader[key] is DBNull)
            {
                return null;
            }

            return (string)reader[key];
        }
    }
}
