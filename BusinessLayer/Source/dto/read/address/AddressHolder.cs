using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace Organisation.BusinessLayer.DTO.Read
{
    [XmlInclude(typeof(Location))]
    [XmlInclude(typeof(Phone))]
    [XmlInclude(typeof(Email))]
    [XmlInclude(typeof(LOSShortName))]
    [XmlInclude(typeof(PostReturn))]
    [XmlInclude(typeof(Contact))]
    [XmlInclude(typeof(EmailRemarks))]
    [XmlInclude(typeof(PhoneHours))]
    [XmlInclude(typeof(ContactHours))]
    [XmlInclude(typeof(Landline))]
    [XmlInclude(typeof(Ean))]
    [XmlInclude(typeof(RacfID))]
    [XmlInclude(typeof(LOSID))]
    [XmlInclude(typeof(Post))]
    [XmlRoot(ElementName = "AddressHolder")]
    [Serializable]
    [DataContract]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Location))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Phone))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Email))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.LOSShortName))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.PostReturn))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Contact))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.EmailRemarks))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.PhoneHours))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.ContactHours))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Ean))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Post))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Url))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.DtrId))]
    [KnownType(typeof(Organisation.BusinessLayer.DTO.Read.Landline))]
    public abstract class AddressHolder
    {
        [DataMember]
        public string Uuid { get; set; }
        [DataMember]
        public string ShortKey { get; set; }
        [DataMember]
        public string Value { get; set; }
    }
}
