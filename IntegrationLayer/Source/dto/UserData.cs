using System;
using System.Collections.Generic;

namespace Organisation.IntegrationLayer
{
    internal class UserData
    {
        public string ShortKey { get; set; }
        public string UserId { get; set; }
        public List<AddressRelation> Addresses { get; set; } = new List<AddressRelation>();
        public DateTime Timestamp { get; set; }
        public string Uuid { get; set; }
        public string PersonUuid { get; set; }
    }
}
