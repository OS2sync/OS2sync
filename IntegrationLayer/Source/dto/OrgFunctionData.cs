using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Organisation.IntegrationLayer
{
    internal class OrgFunctionData
    {
        public string ShortKey { get; set; }
        public string Name { get; set; }
        public string FunctionTypeUuid { get; set; }
        public List<string> Tasks { get; set; } = new List<string>();
        public List<string> Users { get; set; } = new List<string>();
        public List<string> OrgUnits { get; set; } = new List<string>();
        public List<string> ItSystems { get; set; } = new List<string>();
        public List<AddressRelation> Addresses { get; set; } = new List<AddressRelation>();
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
