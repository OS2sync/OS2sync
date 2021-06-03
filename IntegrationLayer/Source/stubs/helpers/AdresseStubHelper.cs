using IntegrationLayer.Adresse;
using System;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class AdresseStubHelper
    {
        internal const string SERVICE = "adresse";
        private static OrganisationRegistryProperties registryProperties = OrganisationRegistryProperties.GetInstance();

        internal void AddProperties(string adresseTekst, string shortKey, VirkningType virkning, RegistreringType1 registration)
        {
            EgenskabType property = new EgenskabType();
            property.AdresseTekst = adresseTekst;
            property.Virkning = virkning;
            property.BrugervendtNoegleTekst = shortKey;

            EgenskabType[] egenskab = new EgenskabType[1];
            egenskab[0] = property;
            registration.AttributListe = egenskab;
        }

        internal AdressePortTypeClient CreatePort()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = Int32.MaxValue;
            binding.OpenTimeout = new TimeSpan(0, 3, 0);
            binding.CloseTimeout = new TimeSpan(0, 3, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 3, 0);
            binding.SendTimeout = new TimeSpan(0, 3, 0);

            AdressePortTypeClient port = new AdressePortTypeClient(binding, StubUtil.GetEndPointAddress("Adresse/5"));
            port.ClientCredentials.ClientCertificate.Certificate = CertificateLoader.LoadCertificateAndPrivateKeyFromFile();

            // Disable revocation checking
            if (registryProperties.DisableRevocationCheck)
            {
                port.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            }

            return port;
        }

        internal RegistreringType1 CreateRegistration(DateTime timestamp, LivscyklusKodeType registrationType)
        {
            UnikIdType systemReference = GetOrganisationReference();
            RegistreringType1 registration = new RegistreringType1();

            registration.Tidspunkt = timestamp;
            registration.TidspunktSpecified = true;
            registration.LivscyklusKode = registrationType;
            registration.LivscyklusKodeSpecified = true;
            registration.BrugerRef = systemReference;

            registration.AttributListe = new EgenskabType[1];
            registration.RelationListe = new RelationListeType();
            registration.TilstandListe = new TilstandListeType();

            return registration;
        }

        private UnikIdType GetOrganisationReference()
        {
            return StubUtil.GetReference<UnikIdType>(StubUtil.GetMunicipalityOrganisationUUID(), ItemChoiceType.UUIDIdentifikator);
        }

        internal VirkningType GetVirkning(DateTime timestamp)
        {
            TidspunktType beginTime = new TidspunktType();
            beginTime.Item = timestamp.Date + new TimeSpan(0, 0, 0);

            VirkningType virkning = new VirkningType();
            virkning.AktoerRef = GetOrganisationReference();
            virkning.AktoerTypeKode = AktoerTypeKodeType.Organisation;
            virkning.AktoerTypeKodeSpecified = true;
            virkning.FraTidspunkt = beginTime;

            return virkning;
        }

        internal AdresseType GetAdresseType(string uuid, RegistreringType1 registration)
        {
            AdresseType organisationType = new AdresseType();
            organisationType.UUIDIdentifikator = uuid;

            RegistreringType1[] registreringTypes = new RegistreringType1[1];
            registreringTypes[0] = registration;
            organisationType.Registrering = registreringTypes;

            return organisationType;
        }
    }
}