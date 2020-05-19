using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Organisation.BusinessLayer.DTO.Registration
{
    [Serializable]
    public class OrgUnitRegistration
    {
        public enum OrgUnitType { DEPARTMENT, TEAM };

        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public string Name { get; set; }
        public string ParentOrgUnitUuid { get; set; }
        public string PayoutUnitUuid { get; set; }
        public string ManagerUuid { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now.AddMinutes(-5);
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public OrgUnitType Type { get; set; }
        public string Location { get; set; }
        public string LOSShortName { get; set; }
        public string LOSId { get; set; }
        public string ContactOpenHours { get; set; }
        public string EmailRemarks { get; set; }
        public string Contact { get; set; }
        public string PostReturn { get; set; }
        public string PhoneOpenHours { get; set; }
        public string Ean { get; set; }
        public string Url { get; set; }
        public string Landline { get; set; }
        public string Post { get; set; }
        public List<string> Tasks { get; set; }
        public List<string> ContactForTasks { get; set; }
    }
}
