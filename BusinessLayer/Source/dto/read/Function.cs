using System;
using System.Runtime.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    public class Function
    {
        public string Uuid { get; set; }
        public string ShortKey { get; set; }
        public Status Status { get; set; }
        public string Name { get; set; }
        public string FunctionType { get; set; }
    }
}
