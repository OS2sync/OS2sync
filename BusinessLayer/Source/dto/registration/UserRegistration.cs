using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Organisation.BusinessLayer.DTO.Registration
{
    [Serializable]
    public class UserRegistration
    {
        // attributes for User object
        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public string UserId { get; set; }

        public string PhoneNumber { get; set; }
        public string Landline { get; set; }
        public string Email { get; set; }
        public string RacfID { get; set;}
        public string Location { get; set; }
        public string FMKID { get; set; }

        public List<Position> Positions { get; set; } = new List<Position>();

        public Person Person { get; set; } = new Person();

        // registration timestamp
        [JsonIgnore]
        public DateTime Timestamp { get; set; } = DateTime.Now.AddMinutes(-5);
    }
}
