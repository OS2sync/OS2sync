using System;
using IntegrationLayer.OrganisationSystem;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationSystemStubHelper
    {
        internal const string SERVICE = "organisationsystem";
        private static OrganisationRegistryProperties registryProperties = OrganisationRegistryProperties.GetInstance();

        internal OrganisationSystemPortTypeClient CreatePort()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = Int32.MaxValue;
            binding.OpenTimeout = new TimeSpan(0, 3, 0);
            binding.CloseTimeout = new TimeSpan(0, 3, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 3, 0);
            binding.SendTimeout = new TimeSpan(0, 3, 0);

            OrganisationSystemPortTypeClient port = new OrganisationSystemPortTypeClient(binding, StubUtil.GetEndPointAddress("OrganisationSystem/5"));
            port.ClientCredentials.ClientCertificate.Certificate = CertificateLoader.LoadCertificateAndPrivateKeyFromFile();

            // Disable revocation checking
            if (registryProperties.DisableRevocationCheck)
            {
                port.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            }

            return port;
        }
    }
}
