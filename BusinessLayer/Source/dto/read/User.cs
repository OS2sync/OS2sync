using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    [KnownType(typeof(Email))]
    [KnownType(typeof(Location))]
    [KnownType(typeof(Phone))]
    public class User
    {
        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public Status Status { get; set; }
        public string UserId { get; set; }
        public Person Person { get; set; }
        public DateTime Timestamp { get; set; }
        public List<Position> Positions { get; set; }
        public List<AddressHolder> Addresses { get; set; }
        public bool IsRobot { get; set; }

        public List<string> Errors { get; set; } = new List<string>();
    }
}
