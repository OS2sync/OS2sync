
namespace Organisation.IntegrationLayer
{
    internal class SslSettings
    {
        public bool Enabled { get; set; } = false;
        public string KeystorePath { get; set; }
        public string KeystorePassword { get; set; }
    }
}
