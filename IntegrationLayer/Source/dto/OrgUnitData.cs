using System;
using System.Collections.Generic;

namespace Organisation.IntegrationLayer
{
    internal class OrgUnitData
    {
        public string ShortKey { get; set; }
        public string Name { get; set; }

        // we need to control the add/remove logic outside the IntegrationLayer, because some functions should be ignored by OS2sync (e.g. udbetalingsenheder)
        // if the user has configured that option - and the type of function is read using the OrganisationFunktionStub, which should not be accessed from
        // OrganisationEnhedStub (so that logic is moved into the BusinessLayer)
        public List<string> OrgFunctionsToAdd { get; set; } = new List<string>();
        public List<string> OrgFunctionsToRemove { get; set; } = new List<string>();

        public List<string> ItSystemUuids { get; set; } = new List<string>();
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
