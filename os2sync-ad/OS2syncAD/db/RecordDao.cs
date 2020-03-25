using System;
using System.Data.SqlClient;

namespace OS2syncAD
{
    public class RecordDao
    {
        private static void Save(Record record)
        {
            bool exists = false;

            using (SqlConnection connection = new SqlConnection(AppConfiguration.DBConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(RecordStatements.SELECT_RECORDS, connection))
                {
                    command.Parameters.Add(new SqlParameter("@key", record.Key));
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        exists = reader.Read();
                    }
                }

                if (exists)
                {
                    using (SqlCommand updateCommand = new SqlCommand(RecordStatements.UPDATE_RECORDS, connection))
                    {
                        updateCommand.Parameters.Add(new SqlParameter("@key", record.Key));
                        updateCommand.Parameters.Add(new SqlParameter("@value", record.Value));
                        updateCommand.Parameters.Add(new SqlParameter("@timestamp", DateTime.Now));

                        updateCommand.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (SqlCommand insertCommand = new SqlCommand(RecordStatements.INSERT_RECORDS, connection))
                    {
                        insertCommand.Parameters.Add(new SqlParameter("@key", record.Key));
                        insertCommand.Parameters.Add(new SqlParameter("@value", record.Value));
                        insertCommand.Parameters.Add(new SqlParameter("@timestamp", DateTime.Now));

                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void Save(string key, string value)
        {
            Record record = new Record();
            record.Key = key;
            record.Value = value;

            Save(record);
        }

        public static Record FindByKey(string key)
        {
            using (SqlConnection connection = new SqlConnection(AppConfiguration.DBConnectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(RecordStatements.SELECT_RECORDS, connection))
                {
                    command.Parameters.Add(new SqlParameter("@key", key));

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            Record record = new Record();

                            record.Id = (long)reader["id"];
                            record.Timestamp = (DateTime)reader["record_timestamp"];
                            record.Key = (string)reader["record_key"];
                            record.Value = (string)reader["record_value"];

                            return record;
                        }
                    }
                }
            }

            return null;
        }
    }
}
