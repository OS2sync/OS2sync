
namespace OS2syncAD
{
    public static class RecordStatements
    {
        public const string SELECT_RECORDS = "SELECT * FROM records where record_key = @key";
        public const string UPDATE_RECORDS = "UPDATE records SET record_value = @value, record_timestamp = @timestamp where record_key = @key";
        public const string INSERT_RECORDS = "INSERT INTO records (record_key, record_value, record_timestamp) VALUES (@key, @value, @timestamp)";
    }
}
