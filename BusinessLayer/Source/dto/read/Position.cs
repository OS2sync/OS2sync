using System;
using System.Runtime.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    public class Position
    {
        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public string Name { get; set; }
        public OUReference OU { get; set; }
        public UserReference User { get; set; }
    }
}
