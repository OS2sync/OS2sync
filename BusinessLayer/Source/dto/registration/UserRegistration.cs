using System;
using System.Collections.Generic;

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
        public string Email { get; set; }
        public string RacfID { get; set;}
        public string Location { get; set; }

        public List<Position> Positions { get; set; } = new List<Position>();

        public Person Person { get; set; } = new Person();

        // registration timestamp
        public DateTime Timestamp { get; set; } = DateTime.Now.AddMinutes(-5);
    }
}
