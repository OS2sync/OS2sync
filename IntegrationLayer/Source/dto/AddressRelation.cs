
using System;

namespace Organisation.IntegrationLayer
{
    internal enum AddressRelationType { POST, EMAIL, EAN, PHONE, LOSSHORTNAME, LOCATION, PHONE_OPEN_HOURS, CONTACT_ADDRESS_OPEN_HOURS, DTR_ID, URL, CONTACT_ADDRESS, POST_RETURN, EMAIL_REMARKS, LANDLINE, LOSID, RACFID, FMKID, FOA, PNR, SOR };

    internal class AddressRelation
    {
        public AddressRelationType Type { get; set; }
        // flip to false on all secondary addresses of same type (right now we only support ONE secondary, and only on POST, but this is generic enough)
        public bool Prime { get; set; } = true;

        private string uuid;
        public string Uuid
        {
            get
            {
                return uuid;
            }
            set
            {
                uuid = value?.ToLower();
            }
        }
    }
}
