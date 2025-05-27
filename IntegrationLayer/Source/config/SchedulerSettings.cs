
using System.Collections.Generic;

namespace Organisation.IntegrationLayer
{
    internal class SchedulerSettings
    {
        public bool Enabled { get; set; } = false;
        public int Threads { get; set; } = 4;
        public string DisableOpgaver { get; set; }
        public string DisableHenvendelsessteder { get; set; } = "true";
        public string DisableUdbetalingsenheder { get; set; } = "true";

        // SOR is ignored by default
        public string IgnoredOUAddressTypes { get; set; } = "SOR";
        public string DBConnectionString { get; set; }
        public string DBType { get; set; } = "MSSQL";
        public string DBMigrationPath { get; set; }
    }
}
