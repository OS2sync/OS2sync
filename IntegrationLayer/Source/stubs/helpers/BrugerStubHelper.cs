using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using IntegrationLayer.Bruger;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class BrugerStubHelper
    {
        private static OrganisationRegistryProperties registryProperties = OrganisationRegistryProperties.GetInstance();
        internal const string SERVICE = "bruger";

        internal void AddProperties(string shortKey, string brugerNavn, VirkningType virkning, RegistreringType1 registration)
        {
            EgenskabType property = new EgenskabType();
            property.BrugervendtNoegleTekst = shortKey;
            property.BrugerNavn = brugerNavn;
            property.Virkning = virkning;

            EgenskabType[] egenskab = new EgenskabType[1];
            egenskab[0] = property;
            registration.AttributListe.Egenskab = egenskab;
        }

        internal bool SetTilstandToActive(VirkningType virkning, RegistreringType1 registration, DateTime timestamp)
        {
            if (TerminateValidityOnGyldighedIfNotMatches(GyldighedStatusKodeType.Aktiv, registration, timestamp))
            {
                GyldighedType gyldighed = GetGyldighedType(GyldighedStatusKodeType.Aktiv, virkning);
                SetTilstand(gyldighed, registration);

                return true;
            }

            return false;
        }

        internal bool SetTilstandToInactive(VirkningType virkning, RegistreringType1 registration, DateTime timestamp)
        {
            if (TerminateValidityOnGyldighedIfNotMatches(GyldighedStatusKodeType.Inaktiv, registration, timestamp))
            {
                GyldighedType gyldighed = GetGyldighedType(GyldighedStatusKodeType.Inaktiv, virkning);
                SetTilstand(gyldighed, registration);

                return true;
            }

            return false;
        }

        private bool TerminateValidityOnGyldighedIfNotMatches(GyldighedStatusKodeType statusCode, RegistreringType1 registration, DateTime timestamp)
        {
            if (registration.TilstandListe.Gyldighed != null && registration.TilstandListe.Gyldighed.Length > 0)
            {
                GyldighedType latestGyldighed = StubUtil.GetLatestGyldighed(registration.TilstandListe.Gyldighed);

                if (statusCode.Equals(latestGyldighed.GyldighedStatusKode))
                {
                    // do nothing, state is already set to what we want
                    return false;
                }

                // otherwise we should terminate the validity before we add a new one
                StubUtil.TerminateVirkning(latestGyldighed.Virkning, timestamp);
            }

            return true;
        }

        private void SetTilstand(GyldighedType gyldighed, RegistreringType1 registration)
        {
            if (registration.TilstandListe.Gyldighed != null && registration.TilstandListe.Gyldighed.Length > 0)
            {
                GyldighedType[] newGyldighedsTyper = new GyldighedType[registration.TilstandListe.Gyldighed.Length + 1];
                int i = 0;

                foreach (GyldighedType oldGyldighed in registration.TilstandListe.Gyldighed)
                {
                    newGyldighedsTyper[i++] = oldGyldighed;
                }

                newGyldighedsTyper[i] = gyldighed;
                registration.TilstandListe.Gyldighed = newGyldighedsTyper;
            }
            else
            {
                registration.TilstandListe.Gyldighed = new GyldighedType[1];
                registration.TilstandListe.Gyldighed[0] = gyldighed;
            }
        }

        internal void AddOrganisationRelation(string organisationUUID, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(organisationUUID, ItemChoiceType.UUIDIdentifikator);

            OrganisationRelationType organisationRelationType = new OrganisationRelationType();
            organisationRelationType.Virkning = virkning;
            organisationRelationType.ReferenceID = orgReference;

            registration.RelationListe.Tilhoerer = organisationRelationType;
        }

        internal GyldighedType GetGyldighedType(GyldighedStatusKodeType type, VirkningType virkning)
        {
            GyldighedType gyldighed = new GyldighedType();
            gyldighed.GyldighedStatusKode = type;
            gyldighed.Virkning = virkning;

            return gyldighed;
        }

        internal BrugerPortTypeClient CreatePort()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = Int32.MaxValue;
            binding.OpenTimeout = new TimeSpan(0, 3, 0);
            binding.CloseTimeout = new TimeSpan(0, 3, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 3, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 3, 0);
            binding.SendTimeout = new TimeSpan(0, 3, 0);

            BrugerPortTypeClient port = new BrugerPortTypeClient(binding, StubUtil.GetEndPointAddress("Bruger/5"));
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

            registration.AttributListe = new AttributListeType();
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

        internal BrugerType GetBrugerType(string uuid, RegistreringType1 registration)
        {
            BrugerType organisationType = new BrugerType();
            organisationType.UUIDIdentifikator = uuid;

            RegistreringType1[] registreringTypes = new RegistreringType1[1];
            registreringTypes[0] = registration;
            organisationType.Registrering = registreringTypes;

            return organisationType;
        }

        internal AdresseFlerRelationType CreateAddressReference(string uuid, int indeks, string roleUuid, VirkningType virkning)
        {
            UnikIdType type = new UnikIdType();
            type.Item = UUIDConstants.ADDRESS_TYPE_USER;
            type.ItemElementName = ItemChoiceType.UUIDIdentifikator;

            UnikIdType role = new UnikIdType();
            role.ItemElementName = ItemChoiceType.UUIDIdentifikator;
            role.Item = roleUuid;

            AdresseFlerRelationType address = new AdresseFlerRelationType();
            address.ReferenceID = StubUtil.GetReference<UnikIdType>(uuid, ItemChoiceType.UUIDIdentifikator);
            address.Virkning = virkning;
            address.Indeks = "" + indeks;
            address.Rolle = role;
            address.Type = type;

            return address;
        }

        internal void AddAddressReferences(List<AddressRelation> references, VirkningType virkning, RegistreringType1 registration)
        {
            if (references == null || references.Count == 0)
            {
                return;
            }

            var adresses = new AdresseFlerRelationType[references.Count];

            int referencesCount = references.Count;
            registration.RelationListe.Adresser = new AdresseFlerRelationType[referencesCount];

            for (int i = 0; i < referencesCount; i++)
            {
                AddressRelation addressRelation = references[i];

                switch (addressRelation.Type)
                {
                    case AddressRelationType.EMAIL:
                        AdresseFlerRelationType emailAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_USER_EMAIL, virkning);
                        registration.RelationListe.Adresser[i] = emailAddress;
                        break;
                    case AddressRelationType.PHONE:
                        AdresseFlerRelationType phoneAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_USER_PHONE, virkning);
                        registration.RelationListe.Adresser[i] = phoneAddress;
                        break;
                    case AddressRelationType.LOCATION:
                        AdresseFlerRelationType locationAddres = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_USER_LOCATION, virkning);
                        registration.RelationListe.Adresser[i] = locationAddres;
                        break;
                    default:
                        throw new Exception("Cannot import Bruger with addressRelationType = " + addressRelation.Type);
                }
            }
        }

        internal void AddPersonRelationship(string personUuid, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType personReference = new UnikIdType();
            personReference.ItemElementName = ItemChoiceType.UUIDIdentifikator;
            personReference.Item = personUuid;

            PersonFlerRelationType person = new PersonFlerRelationType();
            person.Virkning = virkning;
            person.ReferenceID = personReference;

            registration.RelationListe.TilknyttedePersoner = new PersonFlerRelationType[1];
            registration.RelationListe.TilknyttedePersoner[0] = person;
        }

        internal static PersonFlerRelationType GetLatestPersonFlerRelationType(PersonFlerRelationType[] persons)
        {
            if (persons == null || persons.Length == 0)
            {
                return null;
            }

            foreach (PersonFlerRelationType person in persons)
            {
                // find the first open-ended PersonFlerRelationType - objects created by this library does not have end-times associated with them as a rule
                object endTime = person.Virkning.TilTidspunkt.Item;

                // endTime is bool => ok
                // endTime is DateTime, but Now is before endTime => ok
                if (!(endTime is DateTime) || (DateTime.Compare(DateTime.Now, (DateTime)endTime) < 0))
                {
                    return person;
                }
            }

            return null;
        }
    }
}
