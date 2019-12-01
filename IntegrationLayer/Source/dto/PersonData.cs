using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Organisation.IntegrationLayer
{
    internal class PersonData
    {
        public string Name { get; set; }
        public string ShortKey { get; set; }
        public string Cpr { get; set; }
        public DateTime Timestamp { get; set; }

        private string uuid;
        public string Uuid
        {
            get
            {
                return uuid;
            }
            set
            {
                uuid = value?.ToLower();
            }
        }
    }
}
