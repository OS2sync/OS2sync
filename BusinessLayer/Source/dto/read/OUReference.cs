using System;
using System.Runtime.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    public class OUReference
    {
        public string Uuid { get; set; }
        public string Name { get; set; }
    }
}
