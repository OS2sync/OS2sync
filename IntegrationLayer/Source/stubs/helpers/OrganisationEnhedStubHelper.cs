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
        internal const string SERVICE = "organisationenhed/6";

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

        internal bool UpdateItSystemer(List<string> itSystemer, VirkningType virkning, RegistreringType1 registration, DateTime timestamp)
        {
            bool changes = false;

            // make sure we have a list to work with below, and that there are no duplicates
            itSystemer = (itSystemer != null) ? itSystemer : new List<string>();
            itSystemer = itSystemer.Distinct().ToList();

            // find those we need to add (at the end of this method)
            var toAdd = new List<string>();
            foreach (var itSystem in itSystemer)
            {
                bool add = true;

                if (registration.RelationListe?.TilknyttedeItSystemer != null)
                {
                    foreach (var itSystemRelation in registration.RelationListe.TilknyttedeItSystemer)
                    {
                        string uuid = itSystemRelation.ReferenceID?.Item;

                        if (itSystem.Equals(uuid))
                        {
                            add = false;
                            break;
                        }
                    }
                }

                if (add)
                {
                    changes = true;
                    toAdd.Add(itSystem);
                }
            }

            if (registration.RelationListe?.TilknyttedeItSystemer != null)
            {
                IEqualityComparer<ItSystemFlerRelationType> comparer = new ItSystemFlerRelationTypeComparer();

                // remove duplicates from registration.RelationListe.TilknyttedeItSystemer (because sometimes we get duplicates back from KMD)
                registration.RelationListe.TilknyttedeItSystemer = registration.RelationListe.TilknyttedeItSystemer.Distinct(comparer).ToArray();

                // terminate virkning on elements no longer in local
                foreach (var itSystemRelation in registration.RelationListe.TilknyttedeItSystemer)
                {
                    string uuid = itSystemRelation.ReferenceID?.Item;

                    if (uuid != null && !itSystemer.Contains(uuid))
                    {
                        changes = true;
                        StubUtil.TerminateVirkning(itSystemRelation.Virkning, timestamp);
                    }
                }
            }

            // actually add the new ones to this registration
            if (toAdd.Count > 0)
            {
                if (registration.RelationListe.TilknyttedeItSystemer == null || registration.RelationListe.TilknyttedeItSystemer.Length == 0)
                {
                    AddItSystemer(toAdd, virkning, registration);
                }
                else
                {
                    ItSystemFlerRelationType[] itSystemTypes = new ItSystemFlerRelationType[toAdd.Count + registration.RelationListe.TilknyttedeItSystemer.Length];

                    // add new
                    for (int i = 0; i < toAdd.Count; i++)
                    {
                        UnikIdType tilknytteFunktionId = StubUtil.GetReference<UnikIdType>(toAdd[i], ItemChoiceType.UUIDIdentifikator);

                        ItSystemFlerRelationType itSystemType = new ItSystemFlerRelationType();
                        itSystemType.ReferenceID = tilknytteFunktionId;
                        itSystemType.Virkning = virkning;

                        itSystemTypes[i] = itSystemType;
                    }

                    // copy existing
                    for (int j = 0, i = toAdd.Count; i < itSystemTypes.Length && j < registration.RelationListe.TilknyttedeItSystemer.Length; i++, j++)
                    {
                        itSystemTypes[i] = registration.RelationListe.TilknyttedeItSystemer[j];
                    }

                    registration.RelationListe.TilknyttedeItSystemer = itSystemTypes;
                }
            }

            return changes;
        }

        internal bool UpdateOpgaver(List<string> opgaver, VirkningType virkning, RegistreringType1 registration, DateTime timestamp)
        {
            if (!string.IsNullOrEmpty(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableOpgaver))
            {
                // true is global, disabled for all
                if (OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableOpgaver.Contains("true"))
                {
                    return false;
                }

                // check for CVR of current municipality
                if (OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableOpgaver.Contains(OrganisationRegistryProperties.GetCurrentMunicipality()))
                {
                    return false;
                }
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

            // make a copy, needed to make sure we can remove duplicates from FK Organisation
            var opgaverCopy = new List<string>();
            opgaverCopy.AddRange(opgaver);

            // find those to remove (those no longer present in local set)
            var toRemove = new List<int>();
            if (registration.RelationListe?.Opgaver != null)
            {
                for (int i = registration.RelationListe.Opgaver.Length - 1; i>= 0; i--)
                {
                    var opgaveRelation = registration.RelationListe.Opgaver[i];
                    
                    string opgaveRelationUuid = opgaveRelation.ReferenceID?.Item;
                    if (!opgaverCopy.Contains(opgaveRelationUuid))
                    {
                        // we now remove it from opgaverCopy, so we don't match the same value again,
                        // giving us the side-effect of removing duplicates from FK Organisation existing data
                        opgaverCopy.Remove(opgaveRelationUuid);

                        toRemove.Add(i);
                        changes = true;
                    }
                }
            }

            if (changes)
            {
                // generate new set of OpgaverFlerRelationType with correct size
                int newSize = ((registration.RelationListe?.Opgaver != null) ? registration.RelationListe.Opgaver.Length : 0);
                newSize += toAdd.Count;

                int idx = 0;
                OpgaverFlerRelationType[] opgaverTypes = new OpgaverFlerRelationType[newSize];

                int currentMaxIdx = 1;
                // copy existing (but set a stop-date on those flagged for removal)
                if (registration.RelationListe?.Opgaver != null)
                {
                    for (int i = registration.RelationListe.Opgaver.Length - 1; i >= 0; i--)
                    {
                        if (toRemove.Contains(i))
                        {
                            StubUtil.TerminateVirkning(registration.RelationListe.Opgaver[i].Virkning, timestamp);
                        }

                        opgaverTypes[idx++] = registration.RelationListe.Opgaver[i];

                        if (registration.RelationListe.Opgaver[i].Indeks != null)
                        {
                            int idxValue = 0;

                            if (int.TryParse(registration.RelationListe.Opgaver[i].Indeks, out idxValue))
                            {
                                if (idxValue > currentMaxIdx)
                                {
                                    currentMaxIdx = idxValue;
                                }
                            }
                        }
                    }
                }

                // add new
                for (int i = 0; i < toAdd.Count; i++)
                {
                    UnikIdType tilknytteFunktionId = StubUtil.GetReference<UnikIdType>(toAdd[i], ItemChoiceType.UUIDIdentifikator);

                    OpgaverFlerRelationType opgaveType = new OpgaverFlerRelationType();
                    opgaveType.ReferenceID = tilknytteFunktionId;
                    opgaveType.Virkning = virkning;

                    opgaveType.Indeks = (currentMaxIdx++).ToString();

                    // Ansvarlig
                    opgaveType.Rolle = new UuidLabelInputType();
                    opgaveType.Rolle.ItemElementName = ItemChoiceType.UUIDIdentifikator;
                    opgaveType.Rolle.Item = "2a5f38d8-7092-4b3f-85cc-7e272c3c4bb0";

                    // Klasse
                    opgaveType.Type = new UuidLabelInputType();
                    opgaveType.Type.ItemElementName = ItemChoiceType.UUIDIdentifikator;
                    opgaveType.Type.Item = "9870b51e-3bc0-4f98-8827-eba991dd89a9";

                    opgaverTypes[idx++] = opgaveType;
                }

                registration.RelationListe.Opgaver = opgaverTypes;
            }

            return changes;
        }

        internal void AddOpgaver(List<string> opgaver, VirkningType virkning, RegistreringType1 registration)
        {
            if (opgaver == null || opgaver.Count == 0)
            {
                return;
            }

            OpgaverFlerRelationType[] opgaverTypes = new OpgaverFlerRelationType[opgaver.Count];

            for (int i = 0; i < opgaver.Count; i++)
            {
                UnikIdType tilknytteFunktionId = StubUtil.GetReference<UnikIdType>(opgaver[i], ItemChoiceType.UUIDIdentifikator);

                OpgaverFlerRelationType opgaveType = new OpgaverFlerRelationType();
                opgaveType.ReferenceID = tilknytteFunktionId;
                opgaveType.Virkning = virkning;
                opgaveType.Indeks = (i + 1).ToString();

                // Ansvarlig
                opgaveType.Rolle = new UuidLabelInputType();
                opgaveType.Rolle.ItemElementName = ItemChoiceType.UUIDIdentifikator;
                opgaveType.Rolle.Item = "2a5f38d8-7092-4b3f-85cc-7e272c3c4bb0";

                // Klasse
                opgaveType.Type = new UuidLabelInputType();
                opgaveType.Type.ItemElementName = ItemChoiceType.UUIDIdentifikator;
                opgaveType.Type.Item = "9870b51e-3bc0-4f98-8827-eba991dd89a9";

                opgaverTypes[i] = opgaveType;
            }

            registration.RelationListe.Opgaver = opgaverTypes;
        }


        internal void AddOrganisationRelation(string organisationUUID, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType orgReference = StubUtil.GetReference<UnikIdType>(organisationUUID, ItemChoiceType.UUIDIdentifikator);

            OrganisationFlerRelationType organisationRelationType = new OrganisationFlerRelationType();
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

        internal AdresseFlerRelationType CreateAddressReference(string uuid, string roleUuid, bool postAddress, bool primary, VirkningType virkning)
        {
            UuidLabelInputType type = new UuidLabelInputType();
            type.Item = UUIDConstants.ADDRESS_TYPE_ORGUNIT;
            type.ItemElementName = ItemChoiceType.UUIDIdentifikator;

            UuidLabelInputType role = new UuidLabelInputType();
            role.ItemElementName = ItemChoiceType.UUIDIdentifikator;
            role.Item = roleUuid;

            AdresseFlerRelationType address = new AdresseFlerRelationType();
            address.ReferenceID = StubUtil.GetReference<UnikIdType>(uuid, ItemChoiceType.UUIDIdentifikator);
            address.Virkning = virkning;
            address.Rolle = role;
            address.Type = type;

            // we generate a random UUID as index - it is not specced what this is used for, and it just needs to
            // be unique, and it gives us problems if we user a sequential number, as this sometimes overlaps with
            // previous indexes (e.g. when removing and adding addresses in the same update)
            address.Indeks = Guid.NewGuid().ToString().ToLower();

            // note that Post addresses on OU's needs to be sorted, so the primary UUID will be set to (1) in the first
            // character, and the secondary to (2) as a convention to ensure this
            if (postAddress)
            {
                if (primary)
                {
                    address.Indeks = "1" + address.Indeks.Substring(1);
                }
                else
                {
                    address.Indeks = "2" + address.Indeks.Substring(1);
                }
            }

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

        internal void AddItSystemer(List<string> itSystemUuids, VirkningType virkning, RegistreringType1 registration)
        {
            if (itSystemUuids == null || itSystemUuids.Count == 0)
            {
                return;
            }

            // remove duplicates
            itSystemUuids = itSystemUuids.Distinct().ToList();

            registration.RelationListe.TilknyttedeItSystemer = new ItSystemFlerRelationType[itSystemUuids.Count];

            for (int i = 0; i < itSystemUuids.Count; i++)
            {
                ItSystemFlerRelationType itSystem = new ItSystemFlerRelationType();
                itSystem.ReferenceID = new UnikIdType();
                itSystem.ReferenceID.Item = itSystemUuids[i];
                itSystem.ReferenceID.ItemElementName = ItemChoiceType.UUIDIdentifikator;
                itSystem.Virkning = virkning;

                registration.RelationListe.TilknyttedeItSystemer[i] = itSystem;
            }
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

            // we want prime=true first, so we compare y to x because false is sorted before true otherwise
            references.Sort((x, y) => y.Prime.CompareTo(x.Prime));

            for (int i = 0; i < referencesCount; i++)
            {
                AddressRelation addressRelation = references[i];

                switch (addressRelation.Type)
                {
                    case AddressRelationType.EMAIL:
                        AdresseFlerRelationType emailAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = emailAddress;
                        break;
                    case AddressRelationType.PHONE:
                        AdresseFlerRelationType phoneAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = phoneAddress;
                        break;
                    case AddressRelationType.LOCATION:
                        AdresseFlerRelationType locationAddres = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOCATION, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = locationAddres;
                        break;
                    case AddressRelationType.LOSSHORTNAME:
                        AdresseFlerRelationType losAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSSHORTNAME, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = losAddress;
                        break;
                    case AddressRelationType.CONTACT_ADDRESS_OPEN_HOURS:
                        AdresseFlerRelationType contactOpenHoursAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = contactOpenHoursAddress;
                        break;
                    case AddressRelationType.DTR_ID:
                        AdresseFlerRelationType dtrId = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_DTR_ID, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = dtrId;
                        break;
                    case AddressRelationType.URL:
                        AdresseFlerRelationType urlAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_URL, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = urlAddress;
                        break;
                    case AddressRelationType.LANDLINE:
                        AdresseFlerRelationType landlineAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_LANDLINE, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = landlineAddress;
                        break;
                    case AddressRelationType.EAN:
                        AdresseFlerRelationType eanAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_EAN, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = eanAddress;
                        break;
                    case AddressRelationType.LOSID:
                        AdresseFlerRelationType losIdAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSID, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = losIdAddress;
                        break;
                    case AddressRelationType.PHONE_OPEN_HOURS:
                        AdresseFlerRelationType phoneOpenHoursAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE_OPEN_HOURS, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = phoneOpenHoursAddress;
                        break;
                    case AddressRelationType.POST:
                        AdresseFlerRelationType postAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST, true, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = postAddress;
                        break;
                    case AddressRelationType.CONTACT_ADDRESS:
                        AdresseFlerRelationType contactAddress = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = contactAddress;
                        break;
                    case AddressRelationType.EMAIL_REMARKS:
                        AdresseFlerRelationType emailRemarks = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL_REMARKS, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = emailRemarks;
                        break;
                    case AddressRelationType.POST_RETURN:
                        AdresseFlerRelationType postReturn = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST_RETURN, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = postReturn;
                        break;
                    case AddressRelationType.FOA:
                        AdresseFlerRelationType foa = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_FOA, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = foa;
                        break;
                    case AddressRelationType.PNR:
                        AdresseFlerRelationType pnr = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_PNR, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = pnr;
                        break;
                    case AddressRelationType.SOR:
                        AdresseFlerRelationType sor = CreateAddressReference(addressRelation.Uuid, UUIDConstants.ADDRESS_ROLE_ORGUNIT_SOR, false, addressRelation.Prime, virkning);
                        registration.RelationListe.Adresser[i] = sor;
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
    }

    internal class OpgaverFlerRelationTypeComparer : IEqualityComparer<OpgaverFlerRelationType>
    {
        public bool Equals([AllowNull] OpgaverFlerRelationType x, [AllowNull] OpgaverFlerRelationType y)
        {
            if (string.Compare(x?.ReferenceID?.Item, y?.ReferenceID?.Item) == 0)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode([DisallowNull] OpgaverFlerRelationType obj)
        {
            if (obj?.ReferenceID?.Item == null)
            {
                return 0;
            }

            return obj.ReferenceID.Item.GetHashCode();
        }
    }

    internal class ItSystemFlerRelationTypeComparer : IEqualityComparer<ItSystemFlerRelationType>
    {
        public bool Equals([AllowNull] ItSystemFlerRelationType x, [AllowNull] ItSystemFlerRelationType y)
        {
            if (string.Compare(x?.ReferenceID?.Item, y?.ReferenceID?.Item) == 0)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode([DisallowNull] ItSystemFlerRelationType obj)
        {
            if (obj?.ReferenceID?.Item == null)
            {
                return 0;
            }

            return obj.ReferenceID.Item.GetHashCode();
        }
    }
}
