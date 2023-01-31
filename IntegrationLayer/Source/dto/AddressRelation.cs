﻿
namespace Organisation.IntegrationLayer
{
    internal enum AddressRelationType { POST, EMAIL, EAN, PHONE, LOSSHORTNAME, LOCATION, PHONE_OPEN_HOURS, CONTACT_ADDRESS_OPEN_HOURS, DTR_ID, URL, CONTACT_ADDRESS, POST_RETURN, EMAIL_REMARKS, LANDLINE, LOSID, RACFID, FMKID, FOA, PNR, SOR };

    internal class AddressRelation
    {
        public AddressRelationType Type { get; set; }

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
