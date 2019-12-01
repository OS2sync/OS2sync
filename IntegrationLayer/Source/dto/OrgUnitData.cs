using System;
using System.Collections.Generic;

namespace Organisation.IntegrationLayer
{
    internal class OrgUnitData
    {
        public string ShortKey { get; set; }
        public string Name { get; set; }
        public List<string> OrgFunctionUuids { get; set; } = new List<string>();
        public List<AddressRelation> Addresses { get; set; } = new List<AddressRelation>();
        public DateTime Timestamp { get; set; }
        public string OrgUnitType { get; set; } = UUIDConstants.ORGUNIT_TYPE_DEPARTMENT;
        public List<string> Tasks { get; set; }

        private string parentOrgUnitUuid;
        public string ParentOrgUnitUuid
        {
            get
            {
                return parentOrgUnitUuid;
            }
            set
            {
                parentOrgUnitUuid = value?.ToLower();
            }
        }

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
