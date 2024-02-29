
using System.Collections.Generic;

namespace Organisation.IntegrationLayer
{
    internal class SchedulerSettings
    {
        public bool Enabled { get; set; } = false;
        public int Threads { get; set; } = 4;
        public List<string> DisableOpgaver { get; set; } = new List<string>();
        public List<string> DisableHenvendelsessteder { get; set; } = new List<string>() { "true" };
        public List<string> DisableUdbetalingsenheder { get; set; } = new List<string>() { "true" };

        // SOR is ignored by default
        public List<string> IgnoredOUAddressTypes { get; set; } = new List<string>() { "SOR" };
        public string DBConnectionString { get; set; }
        public string DBType { get; set; } = "MSSQL";
        public string DBMigrationPath { get; set; }
    }
}
