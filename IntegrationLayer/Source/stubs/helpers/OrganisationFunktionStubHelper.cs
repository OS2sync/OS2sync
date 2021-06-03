using IntegrationLayer.OrganisationFunktion;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationFunktionStubHelper
    {
        internal const string SERVICE = "organisationfunktion";
        private static OrganisationRegistryProperties registryProperties = OrganisationRegistryProperties.GetInstance();

        internal void AddTilknyttedeEnheder(List<string> tilknyttedeEnheder, VirkningType virkning, RegistreringType1 registration)
        {
            if (tilknyttedeEnheder == null || tilknyttedeEnheder.Count == 0)
            {
                return;
            }

            OrganisationEnhedFlerRelationType[] orgEnhedFlerRelationTypes = new OrganisationEnhedFlerRelationType[tilknyttedeEnheder.Count];

            for (int i = 0; i < tilknyttedeEnheder.Count; i++)
            {
                orgEnhedFlerRelationTypes[i] = CreateOrgEnhedRelation(tilknyttedeEnheder[i], virkning);
            }

            registration.RelationListe.TilknyttedeEnheder = orgEnhedFlerRelationTypes;
        }

        internal OrganisationEnhedFlerRelationType CreateOrgEnhedRelation(string uuid, VirkningType virkning)
        {
            UnikIdType orgEnhedId = StubUtil.GetReference<UnikIdType>(uuid, ItemChoiceType.UUIDIdentifikator);

            OrganisationEnhedFlerRelationType orgEnhedFlerRelationType = new OrganisationEnhedFlerRelationType();
            orgEnhedFlerRelationType.ReferenceID = orgEnhedId;
            orgEnhedFlerRelationType.Virkning = virkning;

            return orgEnhedFlerRelationType;
        }

        internal void AddOrganisationRelation(string organisationUUID, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(organisationUUID, ItemChoiceType.UUIDIdentifikator);

            OrganisationFlerRelationType organisationRelationType = new OrganisationFlerRelationType();
            organisationRelationType.Virkning = virkning;
            organisationRelationType.ReferenceID = orgReference;

            int i = 0;
            OrganisationFlerRelationType[] newRelations = new OrganisationFlerRelationType[1];
            if (registration.RelationListe.TilknyttedeOrganisationer != null && registration.RelationListe.TilknyttedeOrganisationer.Length > 0)
            {
                newRelations = new OrganisationFlerRelationType[registration.RelationListe.TilknyttedeOrganisationer.Length + 1];

                foreach (OrganisationFlerRelationType oldRelation in registration.RelationListe.TilknyttedeOrganisationer)
                {
                    newRelations[i++] = oldRelation;
                }
            }

            registration.RelationListe.TilknyttedeOrganisationer = newRelations;
            registration.RelationListe.TilknyttedeOrganisationer[i] = organisationRelationType;
        }

        internal void AddOpgaver(List<string> klassifikationer, VirkningType virkning, RegistreringType1 registration)
        {
            if (klassifikationer == null || klassifikationer.Count == 0)
            {
                return;
            }

            KlasseFlerRelationType[] klasseFlerRelationTypes = new KlasseFlerRelationType[klassifikationer.Count];

            for (int i = 0; i < klassifikationer.Count; i++)
            {
                klasseFlerRelationTypes[i] = CreateOpgaveRelation(klassifikationer[i], virkning);
            }

            registration.RelationListe.Opgaver = klasseFlerRelationTypes;
        }

        internal KlasseFlerRelationType CreateOpgaveRelation(string uuid, VirkningType virkning)
        {
            UnikIdType klassifikationId = StubUtil.GetReference<UnikIdType>(uuid, ItemChoiceType.UUIDIdentifikator);

            KlasseFlerRelationType klasseFlerRelationType = new KlasseFlerRelationType();
            klasseFlerRelationType.ReferenceID = klassifikationId;
            klasseFlerRelationType.Virkning = virkning;

            return klasseFlerRelationType;
        }

        internal void AddTilknyttedeItSystemer(List<string> itSystems, VirkningType virkning, RegistreringType1 registration)
        {
            if (itSystems == null || itSystems.Count == 0)
            {
                return;
            }

            ItSystemFlerRelationType[] itSystemFlerRelationTypes = new ItSystemFlerRelationType[itSystems.Count];

            for (int i = 0; i < itSystems.Count; i++)
            {
                itSystemFlerRelationTypes[i] = CreateItSystemRelation(itSystems[i], virkning);
            }

            registration.RelationListe.TilknyttedeItSystemer = itSystemFlerRelationTypes;
        }

        internal ItSystemFlerRelationType CreateItSystemRelation(string uuid, VirkningType virkning)
        {
            UnikIdType itSystemId = StubUtil.GetReference<UnikIdType>(uuid, ItemChoiceType.UUIDIdentifikator);

            ItSystemFlerRelationType itSystemFlerRelationType = new ItSystemFlerRelationType();
            itSystemFlerRelationType.ReferenceID = itSystemId;
            itSystemFlerRelationType.Virkning = virkning;

            return itSystemFlerRelationType;
        }

        internal void SetFunktionsType(string funktionsTypeUUID, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType funktionsTypeKlasseId = StubUtil.GetReference<UnikIdType>(funktionsTypeUUID, ItemChoiceType.UUIDIdentifikator);

            KlasseRelationType funktionsType = new KlasseRelationType();
            funktionsType.ReferenceID = funktionsTypeKlasseId;
            funktionsType.Virkning = virkning;

            registration.RelationListe.Funktionstype = funktionsType;
        }

        internal void AddTilknyttedeBrugere(List<string> tilknyttedeBrugere, VirkningType virkning, RegistreringType1 registration)
        {
            if (tilknyttedeBrugere == null || tilknyttedeBrugere.Count == 0)
            {
                return;
            }

            BrugerFlerRelationType[] brugerFlerRelationTypes = new BrugerFlerRelationType[tilknyttedeBrugere.Count];

            for (int i = 0; i < tilknyttedeBrugere.Count; i++)
            {
                brugerFlerRelationTypes[i] = CreateBrugerRelation(tilknyttedeBrugere[i], virkning);
            }

            registration.RelationListe.TilknyttedeBrugere = brugerFlerRelationTypes;
        }

        internal BrugerFlerRelationType CreateBrugerRelation(string uuid, VirkningType virkning)
        {
            UnikIdType orgEnhedId = StubUtil.GetReference<UnikIdType>(uuid, ItemChoiceType.UUIDIdentifikator);

            BrugerFlerRelationType brugerFlerRelationType = new BrugerFlerRelationType();
            brugerFlerRelationType.ReferenceID = orgEnhedId;
            brugerFlerRelationType.Virkning = virkning;

            return brugerFlerRelationType;
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

        internal OrganisationFunktionType GetOrganisationFunktionType(string uuid, RegistreringType1 registration)
        {
            OrganisationFunktionType organisationType = new OrganisationFunktionType();
            organisationType.UUIDIdentifikator = uuid;
            RegistreringType1[] registreringTypes = new RegistreringType1[1];
            registreringTypes[0] = registration;
            organisationType.Registrering = registreringTypes;

            return organisationType;
        }

        internal GyldighedType GetGyldighedType(GyldighedStatusKodeType statusCode, VirkningType virkning)
        {
            GyldighedType gyldighed = new GyldighedType();
            gyldighed.GyldighedStatusKode = statusCode;
            gyldighed.Virkning = virkning;

            return gyldighed;
        }

        internal UnikIdType GetOrganisationReference()
        {
            return StubUtil.GetReference<UnikIdType>(StubUtil.GetMunicipalityOrganisationUUID(), ItemChoiceType.UUIDIdentifikator);
        }

        internal OrganisationFunktionPortTypeClient CreatePort()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            binding.MaxReceivedMessageSize = Int32.MaxValue;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.OpenTimeout = new TimeSpan(0, 3, 0);
            binding.CloseTimeout = new TimeSpan(0, 3, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 3, 0);
            binding.SendTimeout = new TimeSpan(0, 3, 0);

            OrganisationFunktionPortTypeClient port = new OrganisationFunktionPortTypeClient(binding, StubUtil.GetEndPointAddress("OrganisationFunktion/5"));
            port.ClientCredentials.ClientCertificate.Certificate = CertificateLoader.LoadCertificateAndPrivateKeyFromFile();

            // Disable revocation checking
            if (registryProperties.DisableRevocationCheck)
            {
                port.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            }

            return port;
        }

        internal RegistreringType1 CreateRegistration(DateTime timestamp, LivscyklusKodeType livcyklusKodeType)
        {
            UnikIdType systemReference = GetOrganisationReference();
            RegistreringType1 registration = new RegistreringType1();

            registration.Tidspunkt = timestamp;
            registration.TidspunktSpecified = true;
            registration.LivscyklusKode = livcyklusKodeType;
            registration.LivscyklusKodeSpecified = true;
            registration.BrugerRef = systemReference;

            registration.AttributListe = new AttributListeType();
            registration.RelationListe = new RelationListeType();
            registration.TilstandListe = new TilstandListeType();

            return registration;
        }

        internal void AddProperties(String userFacingKey, String funktionNavn, VirkningType virkning, RegistreringType1 registration)
        {
            EgenskabType property = new EgenskabType();
            property.BrugervendtNoegleTekst = userFacingKey;
            property.FunktionNavn = funktionNavn;
            property.Virkning = virkning;

            EgenskabType[] egenskab = new EgenskabType[1];
            egenskab[0] = property;

            registration.AttributListe.Egenskab = egenskab;
        }

        internal AdresseFlerRelationType CreateAddressReference(string uuid, int indeks, string roleUuid, VirkningType virkning)
        {
            UnikIdType type = new UnikIdType();
            type.Item = UUIDConstants.ADDRESS_TYPE_ORGFUNCTION;
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
                    case AddressRelationType.URL:
                        AdresseFlerRelationType urlAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGFUNCTION_URL, virkning);
                        registration.RelationListe.Adresser[i] = urlAddress;
                        break;
                    default:
                        throw new Exception("Cannot import OrganisationFunktion with addressRelationType = " + addressRelation.Type);
                }
            }
        }
    }
}
