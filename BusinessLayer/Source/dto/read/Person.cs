using System;
using System.Runtime.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    public class Person
    {
        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public string Name { get; set; }
        public string Cpr { get; set; }
    }
}
