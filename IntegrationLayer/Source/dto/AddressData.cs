using System;

namespace Organisation.IntegrationLayer
{
    internal class AddressData
    {
        public string ShortKey { get; set; }
        public string AddressText { get; set; }
        public DateTime Timestamp { get; set; }

        private string uuid;
        public string Uuid {
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
