namespace Organisation.IntegrationLayer
{
    internal static class UUIDConstants
    {
        // These UUIDs are copied from "Anvisninger til anvendelse af STS-Organisation v 1.2"

        // These are the UUIDs used to indicate a specific type of address
        public const string ADDRESS_TYPE_ORGUNIT = "9b33c0a0-a566-4ec0-8200-325cb1e5bb9a";
        public const string ADDRESS_TYPE_ORGFUNCTION = "1a8374f6-0ee8-4201-b27c-0e84d57db0ba";
        public const string ADDRESS_TYPE_USER = "71a08d28-3af7-4bb4-9964-bc2b76b93d64";

        // The are the UUIDs used to indicate the role of the address
        public const string ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS = "639a3ed9-86b0-4968-825b-434666cf6220";
        public const string ADDRESS_ROLE_ORGUNIT_POST_RETURN = "3629ce92-594b-4380-9527-81d6e53edc11";
        public const string ADDRESS_ROLE_ORGUNIT_POST = "80b610c6-314b-485a-a014-a9a1d7070bc4";
        public const string ADDRESS_ROLE_ORGUNIT_EMAIL = "2b670843-ce42-411a-8fb5-311dfdd5caf0";
        public const string ADDRESS_ROLE_ORGUNIT_EMAIL_REMARKS = "d4939781-f2c4-401c-8c2f-0d08221127b9";
        public const string ADDRESS_ROLE_ORGUNIT_EAN = "9ccaafe4-c4b2-4d25-942a-2ec5730d4ed8";
        public const string ADDRESS_ROLE_ORGUNIT_URL = "a99c073d-482e-47d3-9275-13c79f453c3a";
        public const string ADDRESS_ROLE_ORGUNIT_PHONE = "8dcfa714-5ed3-4000-b551-2ba520e7d8ad";
        public const string ADDRESS_ROLE_ORGUNIT_LANDLINE = "02826f64-5613-468e-ace5-3f089cb3ed20"; // not used
        public const string ADDRESS_ROLE_ORGUNIT_LOSSHORTNAME = "47a33082-4687-4a68-b82f-5bf6f9d8ee13";
        public const string ADDRESS_ROLE_ORGUNIT_LOCATION = "ec387d90-263c-4cce-8de2-63b407a0daac";
        public const string ADDRESS_ROLE_ORGUNIT_PHONE_OPEN_HOURS = "f6fd6117-c718-4254-a6f5-92538ad5a2f4";
        public const string ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS = "f37e5877-1549-4b5d-a53b-819491f0b933";

        public const string ADDRESS_ROLE_USER_EMAIL = "5d13e891-162a-456b-abf2-fd9b864df96d";
        public const string ADDRESS_ROLE_USER_PHONE = "5ef6be2d-59f4-4652-a680-585929924ba9";
        public const string ADDRESS_ROLE_USER_LANDLINE = "47c05422-5379-4c27-9ddf-e02b52b3d961"; // not used
        public const string ADDRESS_ROLE_USER_LOCATION = "ad04ac80-e24a-45a5-9dd9-8537a916ac74";

        public const string ADDRESS_ROLE_ORGFUNCTION_URL = "560cb83d-386d-43c0-aaa2-986a915b087c";

        // These are the UUIDs used to indicate the type of a function
        public const string ORGFUN_POSITION = "02e61900-33e0-407f-a2a7-22f70221f003";
        public const string ORGFUN_MANAGER = "46c73630-f7ad-4000-9624-c06131cde671"; // not used
        public const string ORGFUN_PAYOUT_UNIT = "faf29ba2-da6d-49c4-8a2f-0739172f4227";
        public const string ORGFUN_CONTACT_UNIT = "7368482a-177e-4e04-8574-f558e6f1ef45";

        // These are the UUIDs used to indicate the type of an OrgUnit
        public const string ORGUNIT_TYPE_DEPARTMENT = "16bf18c3-ed6f-44b0-b7a1-35f94984e519"; // not used
        public const string ORGUNIT_TYPE_TEAM = "2d9710bf-e9cc-465f-8ec7-46d5f2a64412"; // not used
    }
}
