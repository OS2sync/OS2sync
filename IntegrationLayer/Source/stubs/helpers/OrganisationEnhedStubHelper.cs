using System;
using System.Collections.Generic;
using IntegrationLayer.OrganisationEnhed;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationEnhedStubHelper
    {
        internal const string SERVICE = "organisationenhed";
        private static OrganisationRegistryProperties registryProperties = OrganisationRegistryProperties.GetInstance();

        internal void AddTilknyttedeFunktioner(List<string> tilknytteFunktioner, VirkningType virkning, RegistreringType1 registration)
        {
            if (tilknytteFunktioner == null || tilknytteFunktioner.Count == 0)
            {
                return;
            }

            OrganisationFunktionFlerRelationType[] orgFunktionFlerRelationTypes = new OrganisationFunktionFlerRelationType[tilknytteFunktioner.Count];

            for (int i = 0; i < tilknytteFunktioner.Count; i++)
            {
                UnikIdType tilknytteFunktionId = StubUtil.GetReference<UnikIdType>(tilknytteFunktioner[i], ItemChoiceType.UUIDIdentifikator);

                OrganisationFunktionFlerRelationType orgFunktionFlerRelationType = new OrganisationFunktionFlerRelationType();
                orgFunktionFlerRelationType.ReferenceID = tilknytteFunktionId;
                orgFunktionFlerRelationType.Virkning = virkning;

                orgFunktionFlerRelationTypes[i] = orgFunktionFlerRelationType;
            }

            registration.RelationListe.TilknyttedeFunktioner = orgFunktionFlerRelationTypes;
        }

        internal bool UpdateOpgaver(List<string> opgaver, VirkningType virkning, RegistreringType1 registration, DateTime timestamp)
        {
            if (registryProperties.DisableKleOpgaver)
            {
                return false;
            }

            bool changes = false;

            // make sure we have a list to work with below, and that there are no duplicates
            opgaver = (opgaver != null) ? opgaver : new List<string>();
            opgaver = opgaver.Distinct().ToList();

            // find those we need to add (at the end of this method)
            var toAdd = new List<string>();
            foreach (var opgave in opgaver)
            {
                bool add = true;

                if (registration.RelationListe?.Opgaver != null)
                {
                    foreach (var opgaveRelation in registration.RelationListe.Opgaver)
                    {
                        string uuid = opgaveRelation.ReferenceID?.Item;

                        if (opgave.Equals(uuid))
                        {
                            add = false;
                            break;
                        }
                    }
                }

                if (add)
                {
                    changes = true;
                    toAdd.Add(opgave);
                }
            }

            if (registration.RelationListe?.Opgaver != null)
            {
                IEqualityComparer<KlasseFlerRelationType> comparer = new KlasseFlerRelationTypeComparer();

                // remove duplicates from registration.RelationListe.Opgaver (because sometimes we get duplicates back from KMD)
                registration.RelationListe.Opgaver = registration.RelationListe.Opgaver.Distinct(comparer).ToArray();

                // terminate virkning on elements no longer in local
                foreach (var opgaveRelation in registration.RelationListe.Opgaver)
                {
                    string uuid = opgaveRelation.ReferenceID?.Item;

                    if (uuid != null && !opgaver.Contains(uuid))
                    {
                        changes = true;
                        StubUtil.TerminateVirkning(opgaveRelation.Virkning, timestamp);
                    }
                }
            }

            // actually add the new ones to this registration
            if (toAdd.Count > 0)
            {
                if (registration.RelationListe.Opgaver == null || registration.RelationListe.Opgaver.Length == 0)
                {
                    AddOpgaver(toAdd, virkning, registration);
                }
                else
                {
                    KlasseFlerRelationType[] opgaverTypes = new KlasseFlerRelationType[toAdd.Count + registration.RelationListe.Opgaver.Length];

                    // add new
                    for (int i = 0; i < toAdd.Count; i++)
                    {
                        UnikIdType tilknytteFunktionId = StubUtil.GetReference<UnikIdType>(toAdd[i], ItemChoiceType.UUIDIdentifikator);

                        KlasseFlerRelationType opgaveType = new KlasseFlerRelationType();
                        opgaveType.ReferenceID = tilknytteFunktionId;
                        opgaveType.Virkning = virkning;

                        opgaverTypes[i] = opgaveType;
                    }

                    // copy existing
                    for (int j = 0, i = toAdd.Count; i < opgaverTypes.Length && j < registration.RelationListe.Opgaver.Length; i++, j++)
                    {
                        opgaverTypes[i] = registration.RelationListe.Opgaver[j];
                    }

                    registration.RelationListe.Opgaver = opgaverTypes;
                }
            }

            return changes;
        }

        internal void AddOpgaver(List<string> opgaver, VirkningType virkning, RegistreringType1 registration)
        {
            if (opgaver == null || opgaver.Count == 0)
            {
                return;
            }

            KlasseFlerRelationType[] opgaverTypes = new KlasseFlerRelationType[opgaver.Count];

            for (int i = 0; i < opgaver.Count; i++)
            {
                UnikIdType tilknytteFunktionId = StubUtil.GetReference<UnikIdType>(opgaver[i], ItemChoiceType.UUIDIdentifikator);

                KlasseFlerRelationType opgaveType = new KlasseFlerRelationType();
                opgaveType.ReferenceID = tilknytteFunktionId;
                opgaveType.Virkning = virkning;

                opgaverTypes[i] = opgaveType;
            }

            registration.RelationListe.Opgaver = opgaverTypes;
        }


        internal void AddOrganisationRelation(string organisationUUID, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(organisationUUID, ItemChoiceType.UUIDIdentifikator);

            OrganisationRelationType organisationRelationType = new OrganisationRelationType();
            organisationRelationType.Virkning = virkning;
            organisationRelationType.ReferenceID = orgReference;

            registration.RelationListe.Tilhoerer = organisationRelationType;
        }

        internal OrganisationEnhedType GetOrganisationEnhedType(string uuid, RegistreringType1 registration)
        {
            OrganisationEnhedType organisationType = new OrganisationEnhedType();
            organisationType.UUIDIdentifikator = uuid;
            RegistreringType1[] registreringTypes = new RegistreringType1[1];
            registreringTypes[0] = registration;
            organisationType.Registrering = registreringTypes;

            return organisationType;
        }

        internal void AddOverordnetEnhed(string overordnetEnhedUUID, VirkningType virkning, RegistreringType1 registration)
        {
            // allowed to be empty for top-level OU's
            if (String.IsNullOrEmpty(overordnetEnhedUUID))
            {
                return;
            }

            UnikIdType orgUnitReference = StubUtil.GetReference<UnikIdType>(overordnetEnhedUUID, ItemChoiceType.UUIDIdentifikator);

            OrganisationEnhedRelationType organisationEnhedRelationType = new OrganisationEnhedRelationType();
            organisationEnhedRelationType.Virkning = virkning;
            organisationEnhedRelationType.ReferenceID = orgUnitReference;

            registration.RelationListe.Overordnet = organisationEnhedRelationType;
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

        internal RegistreringType1 CreateRegistration(OrgUnitData ou, LivscyklusKodeType livcyklusKodeType)
        {
            UnikIdType systemReference = GetOrganisationReference();
            RegistreringType1 registration = new RegistreringType1();

            registration.Tidspunkt = ou.Timestamp;
            registration.TidspunktSpecified = true;
            registration.LivscyklusKode = LivscyklusKodeType.Importeret;
            registration.LivscyklusKodeSpecified = true;
            registration.BrugerRef = systemReference;
            registration.NoteTekst = (ou.ParentOrgUnitUuid == null) ? "STSOrgSync" : null; // TODO: update according to AP26 once we know how to identify the root OU

            registration.AttributListe = new AttributListeType();
            registration.RelationListe = new RelationListeType();
            registration.TilstandListe = new TilstandListeType();

            return registration;
        }

        internal void AddProperties(String shortKey, String enhedsNavn, VirkningType virkning, RegistreringType1 registration)
        {
            EgenskabType property = new EgenskabType();
            property.BrugervendtNoegleTekst = shortKey;
            property.EnhedNavn = enhedsNavn;
            property.Virkning = virkning;

            EgenskabType[] egenskab = new EgenskabType[1];
            egenskab[0] = property;

            registration.AttributListe.Egenskab = egenskab;
        }

        internal AdresseFlerRelationType CreateAddressReference(string uuid, int indeks, string roleUuid, VirkningType virkning)
        {
            UnikIdType type = new UnikIdType();
            type.Item = UUIDConstants.ADDRESS_TYPE_ORGUNIT;
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

        internal bool SetType(string typeUuid, VirkningType virkning, RegistreringType1 registration)
        {
            if (registration.RelationListe.Enhedstype != null)
            {
                string existingType = registration.RelationListe.Enhedstype.ReferenceID?.Item;

                if (typeUuid.Equals(existingType))
                {
                    return false;
                }

                registration.RelationListe.Enhedstype.ReferenceID.Item = typeUuid;
                registration.RelationListe.Enhedstype.Virkning = virkning;

                return true;
            }

            registration.RelationListe.Enhedstype = new KlasseRelationType();
            registration.RelationListe.Enhedstype.ReferenceID = new UnikIdType();
            registration.RelationListe.Enhedstype.ReferenceID.Item = typeUuid;
            registration.RelationListe.Enhedstype.Virkning = virkning;

            return true;
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
                        AdresseFlerRelationType emailAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL, virkning);
                        registration.RelationListe.Adresser[i] = emailAddress;
                        break;
                    case AddressRelationType.PHONE:
                        AdresseFlerRelationType phoneAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE, virkning);
                        registration.RelationListe.Adresser[i] = phoneAddress;
                        break;
                    case AddressRelationType.LOCATION:
                        AdresseFlerRelationType locationAddres = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOCATION, virkning);
                        registration.RelationListe.Adresser[i] = locationAddres;
                        break;
                    case AddressRelationType.LOSSHORTNAME:
                        AdresseFlerRelationType losAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSSHORTNAME, virkning);
                        registration.RelationListe.Adresser[i] = losAddress;
                        break;
                    case AddressRelationType.CONTACT_ADDRESS_OPEN_HOURS:
                        AdresseFlerRelationType contactOpenHoursAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS, virkning);
                        registration.RelationListe.Adresser[i] = contactOpenHoursAddress;
                        break;
                    case AddressRelationType.DTR_ID:
                        AdresseFlerRelationType dtrId = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_DTR_ID, virkning);
                        registration.RelationListe.Adresser[i] = dtrId;
                        break;
                    case AddressRelationType.URL:
                        AdresseFlerRelationType urlAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_URL, virkning);
                        registration.RelationListe.Adresser[i] = urlAddress;
                        break;
                    case AddressRelationType.LANDLINE:
                        AdresseFlerRelationType landlineAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_LANDLINE, virkning);
                        registration.RelationListe.Adresser[i] = landlineAddress;
                        break;
                    case AddressRelationType.EAN:
                        AdresseFlerRelationType eanAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_EAN, virkning);
                        registration.RelationListe.Adresser[i] = eanAddress;
                        break;
                    case AddressRelationType.LOSID:
                        AdresseFlerRelationType losIdAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSID, virkning);
                        registration.RelationListe.Adresser[i] = losIdAddress;
                        break;
                    case AddressRelationType.PHONE_OPEN_HOURS:
                        AdresseFlerRelationType phoneOpenHoursAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE_OPEN_HOURS, virkning);
                        registration.RelationListe.Adresser[i] = phoneOpenHoursAddress;
                        break;
                    case AddressRelationType.POST:
                        AdresseFlerRelationType postAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST, virkning);
                        registration.RelationListe.Adresser[i] = postAddress;
                        break;
                    case AddressRelationType.CONTACT_ADDRESS:
                        AdresseFlerRelationType contactAddress = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS, virkning);
                        registration.RelationListe.Adresser[i] = contactAddress;
                        break;
                    case AddressRelationType.EMAIL_REMARKS:
                        AdresseFlerRelationType emailRemarks = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL_REMARKS, virkning);
                        registration.RelationListe.Adresser[i] = emailRemarks;
                        break;
                    case AddressRelationType.POST_RETURN:
                        AdresseFlerRelationType postReturn = CreateAddressReference(addressRelation.Uuid, (i + 1), UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST_RETURN, virkning);
                        registration.RelationListe.Adresser[i] = postReturn;
                        break;
                    default:
                        throw new Exception("Cannot import OrganisationEnhed with addressRelationType = " + addressRelation.Type);
                }
            }
        }

        private GyldighedType GetGyldighedType(GyldighedStatusKodeType statusCode, VirkningType virkning)
        {
            GyldighedType gyldighed = new GyldighedType();
            gyldighed.GyldighedStatusKode = statusCode;
            gyldighed.Virkning = virkning;

            return gyldighed;
        }

        private UnikIdType GetOrganisationReference()
        {
            return StubUtil.GetReference<UnikIdType>(StubUtil.GetMunicipalityOrganisationUUID(), ItemChoiceType.UUIDIdentifikator);
        }

        internal OrganisationEnhedPortTypeClient CreatePort()
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.Security.Mode = BasicHttpSecurityMode.Transport;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Certificate;
            binding.MaxReceivedMessageSize = Int32.MaxValue;
            binding.OpenTimeout = new TimeSpan(0, 3, 0);
            binding.CloseTimeout = new TimeSpan(0, 3, 0);
            binding.ReceiveTimeout = new TimeSpan(0, 3, 0);
            binding.SendTimeout = new TimeSpan(0, 3, 0);

            OrganisationEnhedPortTypeClient port = new OrganisationEnhedPortTypeClient(binding, StubUtil.GetEndPointAddress("OrganisationEnhed/5"));
            port.ClientCredentials.ClientCertificate.Certificate = CertificateLoader.LoadCertificateAndPrivateKeyFromFile();

            // Disable revocation checking
            if (registryProperties.DisableRevocationCheck)
            {
                port.ClientCredentials.ServiceCertificate.Authentication.RevocationMode = X509RevocationMode.NoCheck;
            }

            return port;
        }
    }

    internal class KlasseFlerRelationTypeComparer : IEqualityComparer<KlasseFlerRelationType>
    {
        public bool Equals([AllowNull] KlasseFlerRelationType x, [AllowNull] KlasseFlerRelationType y)
        {
            if (string.Compare(x?.ReferenceID?.Item, y?.ReferenceID?.Item) == 0)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode([DisallowNull] KlasseFlerRelationType obj)
        {
            if (obj?.ReferenceID?.Item == null)
            {
                return 0;
            }

            return obj.ReferenceID.Item.GetHashCode();
        }
    }
}
