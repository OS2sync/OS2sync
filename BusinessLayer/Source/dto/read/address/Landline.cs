using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [Serializable]
    [DataContract]
    [XmlRoot(ElementName = "AddressHolder")]
    public class Landline : AddressHolder
    {
        [DataMember]
        public string Type { get { return "Landline"; } }
    }
}
