using Organisation.BusinessLayer.DTO.Registration;
using Organisation.IntegrationLayer;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;

namespace Organisation.SchedulingLayer
{
    public class UserDao
    {
        private string connectionString = null;

        public UserDao()
        {
            connectionString = OrganisationRegistryProperties.GetInstance().DBConnectionString;
        }

        public void Save(UserRegistration user, OperationType operation, string cvr)
        {
            long user_id = 0;

            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (DbCommand command = DaoUtil.GetCommand(UserStatements.Insert, connection))
                        {
                            command.Transaction = transaction;

                            command.Parameters.Add(DaoUtil.GetParameter("@uuid", user.Uuid));
                            command.Parameters.Add(DaoUtil.GetParameter("@shortkey", user.ShortKey ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@user_id", user.UserId ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@phone_number", user.PhoneNumber ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@name", user.Person.Name ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@cpr", user.Person.Cpr ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@email", user.Email ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@racfid", user.RacfID ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@location", user.Location ?? (object)DBNull.Value));
                            command.Parameters.Add(DaoUtil.GetParameter("@cvr", cvr));
                            command.Parameters.Add(DaoUtil.GetParameter("@operation", operation.ToString()));

                            user_id = Convert.ToInt64(command.ExecuteScalar());
                        }

                        // insert positions
                        foreach (Position position in user.Positions ?? Enumerable.Empty<Position>())
                        {
                            using (DbCommand command = DaoUtil.GetCommand(UserStatements.InsertPositions, connection))
                            {
                                command.Transaction = transaction;

                                command.Parameters.Add(DaoUtil.GetParameter("@user_id", user_id));
                                command.Parameters.Add(DaoUtil.GetParameter("@name", position.Name));
                                command.Parameters.Add(DaoUtil.GetParameter("@orgunit_uuid", position.OrgUnitUuid));
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

        public List<UserRegistrationExtended> Get4OldestEntries()
        {
            var users = new List<UserRegistrationExtended>();

            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(UserStatements.Select, connection))
                {
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            UserRegistrationExtended user = new UserRegistrationExtended();
                            long user_id = (long)reader["id"];
                            user.Id = user_id;

                            user.PhoneNumber = GetValue(reader, "phone_number");
                            user.Email = GetValue(reader, "email");
                            user.RacfID = GetValue(reader, "racfid");
                            user.Location = GetValue(reader, "location");

                            user.UserId = GetValue(reader, "user_id");
                            user.Cvr = GetValue(reader, "cvr");
                            user.Uuid = GetValue(reader, "uuid");
                            user.ShortKey = GetValue(reader, "shortkey");

                            user.Person.Name = GetValue(reader, "name");
                            user.Person.Cpr = GetValue(reader, "cpr");
                            user.Timestamp = (DateTime)reader["timestamp"];
                            user.Operation = (OperationType)Enum.Parse(typeof(OperationType), GetValue(reader, "operation"));

                            users.Add(user);
                        }
                    }
                }

                foreach (var user in users)
                {
                    // read positions
                    using (DbCommand command = DaoUtil.GetCommand(UserStatements.SelectPositions, connection))
                    {
                        command.Parameters.Add(DaoUtil.GetParameter("@id", user.Id));

                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Position position = new Position();
                                position.OrgUnitUuid = GetValue(reader, "orgunit_uuid");
                                position.Name = GetValue(reader, "name");

                                user.Positions.Add(position);
                            }
                        }
                    }
                }
            }

            return users;
        }

        public void Delete(long id)
        {
            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(UserStatements.Delete, connection))
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

                using (DbCommand command = DaoUtil.GetCommand(UserStatements.Success, connection))
                {
                    command.Parameters.Add(DaoUtil.GetParameter("@id", id));
                    command.ExecuteNonQuery();
                }
            }
        }

        public void OnFailure(long id, String errorMessage)
        {
            using (DbConnection connection = DaoUtil.GetConnection())
            {
                connection.Open();

                using (DbCommand command = DaoUtil.GetCommand(UserStatements.Failure, connection))
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
