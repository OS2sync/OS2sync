using System;
using System.Collections.Generic;
using static Organisation.BusinessLayer.DTO.Registration.OrgUnitRegistration;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    public class OU
    {
        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; }
        public DateTime Timestamp { get; set; }
        public OUReference ParentOU { get; set; }
        public OUReference PayoutOU { get; set; }
        public UserReference Manager { get; set; }
        public List<AddressHolder> Addresses { get; set; }
        public List<Position> Positions { get; set; }
        public OrgUnitType Type { get; set; }
        public List<string> Tasks { get; set; }
        public List<string> ItSystems { get; set; }
        public List<string> ContactForTasks { get; set; }
        public List<string> ContactPlaces { get; set; }
        public override string ToString() { return this.Name; }

        public List<string> Errors { get; set; } = new List<string>();
    }
}
