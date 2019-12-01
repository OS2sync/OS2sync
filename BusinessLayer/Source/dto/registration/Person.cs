using System;

namespace Organisation.BusinessLayer.DTO.Registration
{
    [Serializable]
    public class Person
    {
        public string Name { get; set; }
        public string Cpr { get; set; }
    }
}
