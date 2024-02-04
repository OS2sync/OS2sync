using System;
using System.Collections.Generic;
using Organisation.IntegrationLayer;
using IntegrationLayer.OrganisationFunktion;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Organisation.BusinessLayer.DTO.Read;
using static Organisation.BusinessLayer.DTO.Registration.OrgUnitRegistration;

namespace Organisation.BusinessLayer
{
    public enum ReadAddresses { YES, NO };
    public enum ReadParentDetails { YES, NO };
    public enum ReadPayoutUnit { YES, NO };
    public enum ReadPositions { YES, NO };
    public enum ReadContactForTasks { YES, NO };
    public enum ReadManager { YES, NO };
    public enum ReadTasks { YES, NO };
    public enum ReadContactPlaces { YES, NO };

    public class InspectorService
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BrugerStub brugerStub = new BrugerStub();
        private OrganisationFunktionStub orgFunctionStub = new OrganisationFunktionStub();
        private AdresseStub adresseStub = new AdresseStub();
        private PersonStub personStub = new PersonStub();
        private OrganisationEnhedStub organisationEnhedStub = new OrganisationEnhedStub();
        private OrganisationSystemStub organisationSystemStub = new OrganisationSystemStub();
        private OrganisationFunktionStub organisationFunktionStub = new OrganisationFunktionStub();
        private readonly object allUnitRolesLock = new object();

        public string ReadUserRaw(string uuid)
        {
            var registration = brugerStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate User with uuid '" + uuid + "'");
            }

            return XmlUtil.SerializeObject(registration);
        }

        public void LoadPositions(List<OU> ous, List<FiltreretOejebliksbilledeType> allUnitRoles)
        {
            foreach (var ou in ous)
            {
                List<Position> positions = new List<Position>();

                ReadPositionsHandler(ou.Uuid, positions, null, allUnitRoles);

                ou.Positions = positions;
            }
        }

        public string ReadFunctionRaw(string uuid)
        {
            var registration = orgFunctionStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate function with uuid '" + uuid + "'");
            }

            return XmlUtil.SerializeObject(registration);
        }

        public string ReadOURaw(string uuid)
        {
            var registration = organisationEnhedStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate OU with uuid '" + uuid + "'");
            }

            return XmlUtil.SerializeObject(registration);
        }

        public string ReadPersonRaw(string uuid)
        {
            var registration = personStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate Person with uuid '" + uuid + "'");
            }

            return XmlUtil.SerializeObject(registration);
        }

        public string ReadAddressRaw(string uuid)
        {
            var registration = adresseStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate Address with uuid '" + uuid + "'");
            }

            return XmlUtil.SerializeObject(registration);
        }

        public Person ReadPersonObject(string uuid)
        {
            global::IntegrationLayer.Person.RegistreringType1 registration = personStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate Person with uuid '" + uuid + "'");
            }
            global::IntegrationLayer.Person.EgenskabType property = StubUtil.GetLatestProperty(registration.AttributListe);

            string cpr = (property != null) ? property.CPRNummerTekst : null;
            string shortKey = (property != null) ? property.BrugervendtNoegleTekst : null;
            string name = (property != null) ? property.NavnTekst : null;

            return new Person()
            {
                Name = name,
                ShortKey = shortKey,
                Uuid = uuid,
                Cpr = cpr
            };
        }

        public Function ReadFunctionObject(string uuid)
        {
            global::IntegrationLayer.OrganisationFunktion.RegistreringType1 registration = orgFunctionStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate Function with uuid '" + uuid + "'");
            }
            global::IntegrationLayer.OrganisationFunktion.EgenskabType property = StubUtil.GetLatestProperty(registration.AttributListe.Egenskab);

            string shortKey = property.BrugervendtNoegleTekst;
            string name = property.FunktionNavn;

            // TODO: perhaps map this to known types?
            string functionType = registration.RelationListe?.Funktionstype?.ReferenceID?.Item;

            // TODO: depending on type, find relevant relations

            Status status = Status.ACTIVE;
            var latestState = StubUtil.GetLatestGyldighed(registration.TilstandListe.Gyldighed);
            if (latestState == null)
            {
                status = Status.UNKNOWN;
            }
            else if (global::IntegrationLayer.OrganisationFunktion.GyldighedStatusKodeType.Inaktiv.Equals(latestState.GyldighedStatusKode))
            {
                status = Status.INACTIVE;
            }

            return new Function()
            {
                Uuid = uuid,
                Name = name,
                ShortKey = shortKey,
                FunctionType = functionType,
                Status = status
            };
        }

        public AddressHolder ReadAddressObject(string uuid)
        {
            global::IntegrationLayer.Adresse.RegistreringType1 registration = adresseStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate Address with uuid '" + uuid + "'");
            }
            global::IntegrationLayer.Adresse.EgenskabType property = StubUtil.GetLatestProperty(registration.AttributListe);

            string shortKey = (property != null) ? property.BrugervendtNoegleTekst : null;
            string value = (property != null) ? property.AdresseTekst : null;

            return new AnonymousAddress()
            {                
                Uuid = uuid,
                Value = value
            };
        }

        public void FindAllUsersInOU(OU ou, List<User> users)
        {
            List<User> newUsers = new List<User>();

            var orgFunctionRegistrations = orgFunctionStub.SoegAndGetLatestRegistration(UUIDConstants.ORGFUN_POSITION, null, ou.Uuid, null);

            if (orgFunctionRegistrations.Count == 0)
            {
                return;
            }

            // extract relevant references into partial user objects
            foreach (var orgFunctionRegistration in orgFunctionRegistrations)
            {
                string orgFunctionName = "Ukendt";

                if (orgFunctionRegistration.Registrering != null && orgFunctionRegistration.Registrering.Length > 0)
                {
                    if (orgFunctionRegistration.Registrering.Length > 1)
                    {
                        log.Warn("More than one registration in output for function: " + orgFunctionRegistration.ObjektType.UUIDIdentifikator);
                    }

                    var orgFunctionProperty = StubUtil.GetLatestProperty(orgFunctionRegistration.Registrering[0].AttributListe.Egenskab);
                    if (orgFunctionProperty != null)
                    {
                        orgFunctionName = orgFunctionProperty.FunktionNavn;
                    }

                    if (orgFunctionRegistration.Registrering[0].RelationListe.TilknyttedeBrugere != null && orgFunctionRegistration.Registrering[0].RelationListe.TilknyttedeBrugere.Length > 0)
                    {
                        // the registration pattern allows for multiple users to share an OrgFunction
                        foreach (var bruger in orgFunctionRegistration.Registrering[0].RelationListe.TilknyttedeBrugere)
                        {
                            bool found = false;
                            User user = new User();

                            // see if we have the user already and throw away the default new user in that case
                            foreach (User u in users)
                            {
                                if (u.Uuid.Equals(bruger.ReferenceID.Item))
                                {
                                    user = u;
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                user.Uuid = bruger.ReferenceID.Item;
                                user.Positions = new List<Position>();
                                users.Add(user);
                                newUsers.Add(user);
                            }

                            user.Positions.Add(new Position()
                            {
                                Name = orgFunctionName,
                                OU = new OUReference()
                                {
                                    Name = ou.Name,
                                    Uuid = ou.Uuid
                                },
                                User = new UserReference()
                                {
                                    Uuid = user.Uuid
                                }
                            });
                        }
                    }
                }
            }

            var personUuids = new List<string>();
            var emailUuids = new List<string>();
            var telephoneUuids = new List<string>();

            // enrich users with User object details
            var userRegistrations = brugerStub.GetLatestRegistrations(newUsers.Select(u => u.Uuid).ToList());
            foreach (var user in newUsers)
            {
                user.Addresses = new List<AddressHolder>();
                user.Person = new Person();

                foreach (var registration in userRegistrations)
                {
                    if (user.Uuid.Equals(registration.Key))
                    {
                        var properties = StubUtil.GetLatestProperty(registration.Value.AttributListe.Egenskab);
                        if (properties != null)
                        {
                            user.UserId = properties.BrugerNavn;
                        }

                        if (registration.Value.RelationListe.Adresser != null)
                        {
                            foreach (var address in registration.Value.RelationListe.Adresser)
                            {
                                if (UUIDConstants.ADDRESS_ROLE_USER_EMAIL.Equals(address.Rolle.Item))
                                {
                                    emailUuids.Add(address.ReferenceID.Item);

                                    user.Addresses.Add(new Email()
                                    {
                                        Uuid = address.ReferenceID.Item                                        
                                    });
                                }
                                else if (UUIDConstants.ADDRESS_ROLE_USER_PHONE.Equals(address.Rolle.Item))
                                {
                                    telephoneUuids.Add(address.ReferenceID.Item);

                                    user.Addresses.Add(new Phone()
                                    {
                                        Uuid = address.ReferenceID.Item
                                    });
                                }
                            }
                        }

                        if (registration.Value.RelationListe.TilknyttedePersoner != null && registration.Value.RelationListe.TilknyttedePersoner.Length > 0)
                        {
                            user.Person.Uuid = registration.Value.RelationListe.TilknyttedePersoner[0].ReferenceID.Item;

                            personUuids.Add(registration.Value.RelationListe.TilknyttedePersoner[0].ReferenceID.Item);
                        }

                        break;
                    }
                }
            }

            // enrich with email address information
            if (emailUuids.Count > 0)
            {
                var emailRegistrations = adresseStub.GetLatestRegistrations(emailUuids);
                foreach (var user in newUsers)
                {
                    foreach (var address in user.Addresses)
                    {
                        foreach (var emailRegistration in emailRegistrations)
                        {
                            if (emailRegistration.Key.Equals(address.Uuid))
                            {
                                var properties = StubUtil.GetLatestProperty(emailRegistration.Value.AttributListe);
                                if (properties != null)
                                {
                                    address.Value = properties.AdresseTekst;
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // enrich with telephone address information
            if (telephoneUuids.Count > 0)
            {
                var phoneRegistrations = adresseStub.GetLatestRegistrations(telephoneUuids);
                foreach (var user in newUsers)
                {
                    foreach (var address in user.Addresses)
                    {
                        foreach (var phoneRegistration in phoneRegistrations)
                        {
                            if (phoneRegistration.Key.Equals(address.Uuid))
                            {
                                var properties = StubUtil.GetLatestProperty(phoneRegistration.Value.AttributListe);
                                if (properties != null)
                                {
                                    address.Value = properties.AdresseTekst;
                                }

                                break;
                            }
                        }
                    }
                }
            }

            // enrich with person information
            var personRegistrations = personStub.GetLatestRegistrations(personUuids);
            foreach (var user in newUsers)
            {
                foreach (var personRegistration in personRegistrations)
                {
                    if (personRegistration.Key.Equals(user.Person?.Uuid))
                    {
                        var properties = StubUtil.GetLatestProperty(personRegistration.Value.AttributListe);
                        if (properties != null)
                        {
                            user.Person.Name = properties.NavnTekst;
                        }

                        break;
                    }
                }
            }
        }

        public List<User> ReadUserObjects(List<string> uuids, List<global::IntegrationLayer.OrganisationFunktion.FiltreretOejebliksbilledeType> allUnitRoles, ReadAddresses readAddresses = ReadAddresses.YES, ReadParentDetails readParentDetails = ReadParentDetails.YES)
        {
            var result = new List<User>();
            var addressesToFetch = new Dictionary<string, List<global::IntegrationLayer.Bruger.AdresseFlerRelationType>>();
            var personsToFetch = new List<string>();

            var registrations = brugerStub.GetLatestRegistrations(uuids);
            foreach (string uuid in uuids)
            {
                List<string> errors = new List<string>();

                if (!registrations.ContainsKey(uuid))
                {
                    errors.Add("Could not locate User with uuid '" + uuid + "'");

                    result.Add(new User()
                    {
                        Uuid = uuid,
                        Status = Status.UNKNOWN,
                        Errors = errors
                    });

                    continue;
                }

                var registration = registrations[uuid];
                DateTime timestamp = registration.Tidspunkt;

                global::IntegrationLayer.Bruger.EgenskabType property = StubUtil.GetLatestProperty(registration.AttributListe.Egenskab);

                string userId = (property != null) ? property.BrugerNavn : null;
                string userShortKey = (property != null) ? property.BrugervendtNoegleTekst : null;

                // any addresses to fetch in bulk at a later time?
                if (registration.RelationListe?.Adresser != null)
                {
                    foreach (global::IntegrationLayer.Bruger.AdresseFlerRelationType address in registration.RelationListe.Adresser)
                    {
                        if (!addressesToFetch.ContainsKey(uuid))
                        {
                            addressesToFetch.Add(uuid, new List<global::IntegrationLayer.Bruger.AdresseFlerRelationType>());
                        }

                        addressesToFetch[uuid].Add(address);
                    }
                }

                Person person = null;
                if (registration.RelationListe?.TilknyttedePersoner != null && registration.RelationListe?.TilknyttedePersoner.Length > 0)
                {
                    person = new Person();
                    person.Uuid = registration.RelationListe.TilknyttedePersoner[0].ReferenceID.Item;

                    personsToFetch.Add(person.Uuid);
                }

                List<Position> positions = new List<Position>();
                List<FiltreretOejebliksbilledeType> unitRoles = ServiceHelper.FindUnitRolesForUser(uuid, allUnitRoles);
                if (unitRoles != null && unitRoles.Count > 0)
                {
                    foreach (var unitRole in unitRoles)
                    {
                        string orgFunctionName = "OrgFunction object does not exist in Organisation";
                        string orgFunctionShortKey = "OrgFunction object does not exist in Organisation";
                        string startDate = null, stopDate = null;

                        OUReference ou = new OUReference()
                        {
                            Uuid = null,
                            Name = "OrgUnit object does not exist in Organisation"
                        };

                        string positionUuid = unitRole.ObjektType.UUIDIdentifikator;
                        RegistreringType1 orgFunctionRegistration = unitRole.Registrering[0];
                        if (orgFunctionRegistration != null)
                        {
                            global::IntegrationLayer.OrganisationFunktion.EgenskabType orgFunctionProperty = StubUtil.GetLatestProperty(orgFunctionRegistration.AttributListe.Egenskab);

                            if (orgFunctionProperty != null)
                            {
                                orgFunctionName = orgFunctionProperty.FunktionNavn;
                                orgFunctionShortKey = orgFunctionProperty.BrugervendtNoegleTekst;
                            }

                            if (orgFunctionRegistration.RelationListe.TilknyttedeEnheder != null && orgFunctionRegistration.RelationListe.TilknyttedeEnheder.Length > 0)
                            {
                                global::IntegrationLayer.OrganisationFunktion.OrganisationEnhedFlerRelationType parentOu = orgFunctionRegistration.RelationListe.TilknyttedeEnheder[0];
                                string parentOuUuid = parentOu.ReferenceID.Item;
                                ou.Uuid = parentOuUuid;

                                if (parentOu.Virkning?.FraTidspunkt?.Item is DateTime)
                                {
                                    startDate = ((DateTime)parentOu.Virkning.FraTidspunkt.Item).ToString("yyyy-MM-dd");                                   
                                }
                                if (parentOu.Virkning?.TilTidspunkt?.Item is DateTime)
                                {
                                    stopDate = ((DateTime)parentOu.Virkning.TilTidspunkt.Item).ToString("yyyy-MM-dd");
                                }

                                if (readParentDetails.Equals(ReadParentDetails.YES))
                                {
                                    global::IntegrationLayer.OrganisationEnhed.RegistreringType1 parentRegistration = organisationEnhedStub.GetLatestRegistration(parentOuUuid);
                                    if (parentRegistration != null)
                                    {
                                        global::IntegrationLayer.OrganisationEnhed.EgenskabType parentProperties = StubUtil.GetLatestProperty(parentRegistration.AttributListe.Egenskab);
                                        if (parentProperties != null)
                                        {
                                            ou.Name = parentProperties.EnhedNavn;
                                        }
                                    }
                                    else
                                    {
                                        errors.Add("Employeed in non-existing OU: " + parentOuUuid);
                                    }
                                }
                            }
                        }

                        Position position = new Position()
                        {
                            Name = orgFunctionName,
                            OU = ou,
                            ShortKey = orgFunctionShortKey,
                            Uuid = positionUuid,
                            StartDate = startDate,
                            StopDate = stopDate
                        };

                        positions.Add(position);
                    }
                }

                Status status = Status.ACTIVE;
                var latestState = StubUtil.GetLatestGyldighed(registration.TilstandListe.Gyldighed);
                if (latestState == null)
                {
                    errors.Add("No Tilstand set on object!");
                    status = Status.UNKNOWN;
                }
                else if (global::IntegrationLayer.Bruger.GyldighedStatusKodeType.Inaktiv.Equals(latestState.GyldighedStatusKode))
                {
                    status = Status.INACTIVE;
                }

                result.Add(new User()
                {
                    ShortKey = userShortKey,
                    Uuid = uuid,
                    UserId = userId,
                    Addresses = new List<AddressHolder>(),
                    Person = person,
                    Positions = positions,
                    Status = status,
                    Timestamp = timestamp,
                    Errors = errors
                });
            }

            // bulk read person objets
            var persons = personStub.GetLatestRegistrations(personsToFetch);

            foreach (var user in result)
            {
                foreach (var key in persons.Keys)
                {
                    if (key.Equals(user.Person?.Uuid))
                    {
                        var registration = persons[key];

                        user.Person = MapRegistrationToPerson(user.Person.Uuid, registration, user.Errors);

                        break;
                    }
                }
            }

            // bulk read addresses
            if (readAddresses.Equals(ReadAddresses.YES))
            {
                var allAdressesToRead = new List<global::IntegrationLayer.Bruger.AdresseFlerRelationType>();

                // convert to one long list
                foreach (string key in addressesToFetch.Keys)
                {
                    foreach (var address in addressesToFetch[key])
                    {
                        allAdressesToRead.Add(address);
                    }
                }

                var allReadAddresses = mapAddresses(allAdressesToRead);

                foreach (User user in result)
                {
                    if (addressesToFetch.ContainsKey(user.Uuid))
                    {
                        var matchSet = addressesToFetch[user.Uuid];

                        var addresses = new List<AddressHolder>();
                        foreach (var match in matchSet)
                        {
                            string addressUuid = match.ReferenceID.Item;

                            foreach (var readAddress in allReadAddresses)
                            {
                                if (readAddress.Uuid.Equals(addressUuid))
                                {
                                    addresses.Add(readAddress);
                                }
                            }
                        }

                        user.Addresses = addresses;
                    }
                }
            }

            return result;
        }

        private Person readPerson(global::IntegrationLayer.Bruger.PersonFlerRelationType personRelation, List<string> errors)
        {
            string personUuid = personRelation.ReferenceID.Item;

            global::IntegrationLayer.Person.RegistreringType1 personRegistration = personStub.GetLatestRegistration(personUuid);

            return MapRegistrationToPerson(personUuid, personRegistration, errors);
        }

        private Person MapRegistrationToPerson(string uuid, global::IntegrationLayer.Person.RegistreringType1 personRegistration, List<string> errors)
        {
            string personName = "The person object does not exist in Organisation";
            string personShortKey = "The person object does not exist in Organisation";
            string personCpr = "The person object does not exist in Organisation";

            if (personRegistration != null)
            {
                global::IntegrationLayer.Person.EgenskabType personProperty = StubUtil.GetLatestProperty(personRegistration.AttributListe);

                if (personProperty != null)
                {
                    personName = personProperty.NavnTekst;
                    personShortKey = personProperty.BrugervendtNoegleTekst;
                    personCpr = personProperty.CPRNummerTekst;
                }
            }
            else if (errors != null)
            {
                errors.Add("Reference to non-existing person: " + uuid);
            }

            return new Person()
            {
                Name = personName,
                ShortKey = personShortKey,
                Cpr = personCpr,
                Uuid = uuid
            };
        }

        private List<AddressHolder> mapAddresses(List<global::IntegrationLayer.Bruger.AdresseFlerRelationType> addressList)
        {
            List<AddressHolder> addresses = new List<AddressHolder>();

            List<string> uuids = new List<string>();
            foreach (var addressEntry in addressList)
            {
                uuids.Add(addressEntry.ReferenceID.Item);
            }

            var addressRegistrations = adresseStub.GetLatestRegistrations(uuids);

            foreach (var address in addressList)
            {
                string addressUuid = address.ReferenceID.Item;

                if (!addressRegistrations.ContainsKey(addressUuid))
                {
                    log.Warn("Could not find address: " + addressUuid);
                    continue;
                }

                var addressRegistration = addressRegistrations[addressUuid];

                string addressValue = "The address object does not exist in Organisation";
                string addressShortKey = "";

                if (addressRegistration != null)
                {
                    global::IntegrationLayer.Adresse.EgenskabType addressProperty = StubUtil.GetLatestProperty(addressRegistration.AttributListe);

                    if (addressProperty != null)
                    {
                        addressValue = addressProperty.AdresseTekst;
                        addressShortKey = addressProperty.BrugervendtNoegleTekst;
                    }
                }

                if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_EMAIL))
                {
                    addresses.Add(new Email()
                    {
                        Uuid = addressUuid,
                        ShortKey = addressShortKey,
                        Value = addressValue
                    });
                }
                else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_RACFID))
                {
                    addresses.Add(new RacfID()
                    {
                        Uuid = addressUuid,
                        ShortKey = addressShortKey,
                        Value = addressValue
                    });
                }
                else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_LOCATION))
                {
                    addresses.Add(new Location()
                    {
                        Uuid = addressUuid,
                        ShortKey = addressShortKey,
                        Value = addressValue
                    });
                }
                else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_LANDLINE))
                {
                    addresses.Add(new Landline()
                    {
                        Uuid = addressUuid,
                        ShortKey = addressShortKey,
                        Value = addressValue
                    });
                }
                else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_PHONE))
                {
                    addresses.Add(new Phone()
                    {
                        Uuid = addressUuid,
                        ShortKey = addressShortKey,
                        Value = addressValue
                    });
                }
                else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_FMKID))
                {
                    addresses.Add(new FMKID()
                    {
                        Uuid = addressUuid,
                        ShortKey = addressShortKey,
                        Value = addressValue
                    });
                }
            }

            return addresses;
        }

        private void mapAddress(global::IntegrationLayer.Bruger.AdresseFlerRelationType address, List<AddressHolder> addresses, List<string> errors)
        {
            string addressUuid = address.ReferenceID.Item;
            global::IntegrationLayer.Adresse.RegistreringType1 addressRegistration = adresseStub.GetLatestRegistration(addressUuid);

            string addressValue = "The address object does not exist in Organisation";
            string addressShortKey = "";
            if (addressRegistration != null)
            {
                global::IntegrationLayer.Adresse.EgenskabType addressProperty = StubUtil.GetLatestProperty(addressRegistration.AttributListe);

                if (addressProperty != null)
                {
                    addressValue = addressProperty.AdresseTekst;
                    addressShortKey = addressProperty.BrugervendtNoegleTekst;
                }
            }
            else
            {
                errors.Add("Reference to non-existing address: " + addressUuid);
            }

            if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_EMAIL))
            {
                addresses.Add(new Email()
                {
                    Uuid = addressUuid,
                    ShortKey = addressShortKey,
                    Value = addressValue
                });
            }
            else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_RACFID))
            {
                addresses.Add(new RacfID()
                {
                    Uuid = addressUuid,
                    ShortKey = addressShortKey,
                    Value = addressValue
                });
            }
            else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_LOCATION))
            {
                addresses.Add(new Location()
                {
                    Uuid = addressUuid,
                    ShortKey = addressShortKey,
                    Value = addressValue
                });
            }
            else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_LANDLINE))
            {
                addresses.Add(new Landline()
                {
                    Uuid = addressUuid,
                    ShortKey = addressShortKey,
                    Value = addressValue
                });
            }
            else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_PHONE))
            {
                addresses.Add(new Phone()
                {
                    Uuid = addressUuid,
                    ShortKey = addressShortKey,
                    Value = addressValue
                });
            }
            else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_FMKID))
            {
                addresses.Add(new FMKID()
                {
                    Uuid = addressUuid,
                    ShortKey = addressShortKey,
                    Value = addressValue
                });
            }
            else
            {
                errors.Add("Unknown address rolle: " + address.Rolle.Item);
            }
        }

        public OU ReadOUObject(string uuid, ReadTasks readTasks = ReadTasks.YES, ReadManager readManager = ReadManager.YES, ReadAddresses readAddress = ReadAddresses.YES, ReadPayoutUnit readPayoutUnit = ReadPayoutUnit.YES, ReadContactPlaces readContactPlaces = ReadContactPlaces.YES, ReadPositions readPositions = ReadPositions.YES, ReadContactForTasks readContactForTasks = ReadContactForTasks.YES)
        {
            global::IntegrationLayer.OrganisationEnhed.RegistreringType1 registration = organisationEnhedStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate OU with uuid '" + uuid + "'");
            }

            return MapRegistrationToOU(registration, uuid, null, readTasks, readManager, readAddress, readPayoutUnit, readContactPlaces, readPositions, readContactForTasks);
        }

        private OU MapRegistrationToOU(dynamic registration, string uuid, List<FiltreretOejebliksbilledeType> allUnitRoles, ReadTasks readTasks = ReadTasks.YES, ReadManager readManager = ReadManager.YES, ReadAddresses readAddresses = ReadAddresses.YES, ReadPayoutUnit readPayoutUnit = ReadPayoutUnit.YES, ReadContactPlaces readContactPlaces = ReadContactPlaces.YES, ReadPositions readPositions = ReadPositions.YES, ReadContactForTasks readContactForTasks = ReadContactForTasks.YES)
        {
            var wrapper = new OrgUnitRegWrapper();
            wrapper.Registration = registration;
            wrapper.Uuid = uuid;

            var wrappers = new List<OrgUnitRegWrapper>();
            wrappers.Add(wrapper);

            var result = MapRegistrationsToOUs(wrappers, allUnitRoles, readTasks, readManager, readAddresses, readPayoutUnit, readContactPlaces, readPositions, readContactForTasks);

            if (result.Count > 0)
            {
                return result[0];
            }

            // this will not happen... I'm pretty sure ;)
            return null;
        }

        public User ReadUserObject(string uuid, ReadAddresses readAddresses = ReadAddresses.YES, ReadParentDetails readParentDetails = ReadParentDetails.YES)
        {
            List<string> errors = new List<string>();

            global::IntegrationLayer.Bruger.RegistreringType1 registration = brugerStub.GetLatestRegistration(uuid);
            if (registration == null)
            {
                throw new RegistrationNotFoundException("Could not locate User with uuid '" + uuid + "'");
            }

            DateTime timestamp = registration.Tidspunkt;

            global::IntegrationLayer.Bruger.EgenskabType property = StubUtil.GetLatestProperty(registration.AttributListe.Egenskab);

            string userId = (property != null) ? property.BrugerNavn : null;
            string userShortKey = (property != null) ? property.BrugervendtNoegleTekst : null;

            List<AddressHolder> addresses = new List<AddressHolder>();
            if (readAddresses.Equals(ReadAddresses.YES))
            {
                if (registration.RelationListe?.Adresser != null)
                {
                    foreach (global::IntegrationLayer.Bruger.AdresseFlerRelationType address in registration.RelationListe.Adresser)
                    {
                        mapAddress(address, addresses, errors);
                    }
                }
            }

            Person person = null;
            if (registration.RelationListe?.TilknyttedePersoner != null && registration.RelationListe?.TilknyttedePersoner.Length > 0)
            {
                person = readPerson(registration.RelationListe.TilknyttedePersoner[0], errors);
            }

            List<Position> positions = new List<Position>();
            List<FiltreretOejebliksbilledeType> unitRoles = ServiceHelper.FindUnitRolesForUser(uuid);
            if (unitRoles != null && unitRoles.Count > 0)
            {
                foreach (var unitRole in unitRoles)
                {
                    string orgFunctionName = "OrgFunction object does not exist in Organisation";
                    string orgFunctionShortKey = "OrgFunction object does not exist in Organisation";
                    string startDate = null, stopDate = null;

                    OUReference ou = new OUReference()
                    {
                        Uuid = null,
                        Name = "OrgUnit object does not exist in Organisation"
                    };

                    string positionUuid = unitRole.ObjektType.UUIDIdentifikator;
                    RegistreringType1 orgFunctionRegistration = unitRole.Registrering[0];
                    if (orgFunctionRegistration != null)
                    {
                        global::IntegrationLayer.OrganisationFunktion.EgenskabType orgFunctionProperty = StubUtil.GetLatestProperty(orgFunctionRegistration.AttributListe.Egenskab);

                        if (orgFunctionProperty != null)
                        {
                            orgFunctionName = orgFunctionProperty.FunktionNavn;
                            orgFunctionShortKey = orgFunctionProperty.BrugervendtNoegleTekst;
                        }

                        if (orgFunctionRegistration.RelationListe.TilknyttedeEnheder != null && orgFunctionRegistration.RelationListe.TilknyttedeEnheder.Length > 0)
                        {
                            global::IntegrationLayer.OrganisationFunktion.OrganisationEnhedFlerRelationType parentOu = orgFunctionRegistration.RelationListe.TilknyttedeEnheder[0];
                            string parentOuUuid = parentOu.ReferenceID.Item;
                            ou.Uuid = parentOuUuid;

                            if (parentOu.Virkning?.FraTidspunkt?.Item is DateTime)
                            {
                                startDate = ((DateTime)parentOu.Virkning.FraTidspunkt.Item).ToString("yyyy-MM-dd");
                            }

                            if (parentOu.Virkning?.TilTidspunkt?.Item is DateTime)
                            {
                                stopDate = ((DateTime)parentOu.Virkning.TilTidspunkt.Item).ToString("yyyy-MM-dd");
                            }

                            if (readParentDetails.Equals(ReadParentDetails.YES))
                            {
                                global::IntegrationLayer.OrganisationEnhed.RegistreringType1 parentRegistration = organisationEnhedStub.GetLatestRegistration(parentOuUuid);
                                if (parentRegistration != null)
                                {
                                    global::IntegrationLayer.OrganisationEnhed.EgenskabType parentProperties = StubUtil.GetLatestProperty(parentRegistration.AttributListe.Egenskab);
                                    if (parentProperties != null)
                                    {
                                        ou.Name = parentProperties.EnhedNavn;
                                    }
                                }
                                else
                                {
                                    errors.Add("Employeed in non-existing OU: " + parentOuUuid);
                                }
                            }
                        }
                    }

                    Position position = new Position()
                    {
                        Name = orgFunctionName,
                        OU = ou,
                        ShortKey = orgFunctionShortKey,
                        Uuid = positionUuid,
                        StartDate = startDate,
                        StopDate = stopDate
                    };

                    positions.Add(position);
                }
            }

            Status status = Status.ACTIVE;
            var latestState = StubUtil.GetLatestGyldighed(registration.TilstandListe.Gyldighed);
            if (latestState == null)
            {
                errors.Add("No Tilstand set on object!");
                status = Status.UNKNOWN;
            }
            else if (global::IntegrationLayer.Bruger.GyldighedStatusKodeType.Inaktiv.Equals(latestState.GyldighedStatusKode))
            {
                status = Status.INACTIVE;
            }

            return new User()
            {
                ShortKey = userShortKey,
                Uuid = uuid,
                UserId = userId,
                Addresses = addresses,
                Person = person,
                Positions = positions,
                Status = status,
                Timestamp = timestamp,
                Errors = errors
            };
        }

        public List<string> FindAllUsers(List<OU> ous = null)
        {
            List<string> result = new List<string>();

            if (ous != null)
            {
                foreach (var ou in ous)
                {
                    if (ou.Positions != null)
                    {
                        foreach (var position in ou.Positions)
                        {
                            result.Add(position.User.Uuid);
                        }
                    }
                }
            }
            else
            {
                result = brugerStub.Soeg();
            }

            return result;
        }

        public List<string> FindAllOUs()
        {
            return organisationEnhedStub.Soeg();
        }

        public List<User> ReadUsers(string cvr, List<string> users, List<FiltreretOejebliksbilledeType> allUnitRoles, Func<long, long, bool> progressCallback, ReadAddresses readAddresses = ReadAddresses.YES, ReadParentDetails readParentDetails = ReadParentDetails.NO)
        {
            List<User> result = new List<User>();

            // put into sets of 50 uuids a pop - so we can bulk operate on them
            var usersInBulk = new List<List<string>>();
            var currentList = new List<string>();
            usersInBulk.Add(currentList);

            int counter = 0;
            int total = 0;

            foreach (string uuid in users)
            {
                currentList.Add(uuid);
                counter++;
                total++;

                if (counter >= 50)
                {
                    currentList = new List<string>();
                    usersInBulk.Add(currentList);
                    counter = 0;
                }
            }

            Parallel.ForEach(usersInBulk, new ParallelOptions { MaxDegreeOfParallelism = 6 }, (uuids) =>
            {
                // set cvr on thread
                OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

                int count = 0;

                if (uuids.Count > 0)
                {
                    while (true)
                    {
                        try
                        {
                            var tmpResult = ReadUserObjects(uuids, allUnitRoles, readAddresses, readParentDetails);

                            ReadUsersUpdateProgress(tmpResult, result);

                            progressCallback?.Invoke(tmpResult.Count, total);

                            break;
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Timeout: " + ex.Message);

                            count++;
                            if (count >= 3)
                            {
                                log.Error("Failed to read user data - creating dummy users in report", ex);

                                List<User> tmpResult = new List<User>();

                                foreach (string uuid in uuids)
                                {
                                    var errors = new List<string>();
                                    errors.Add("Connection error on user with uuid '" + uuid + "'");

                                    User user = new User();
                                    user.Uuid = uuid;
                                    user.Status = Status.UNKNOWN;
                                    user.Person = new Person();
                                    user.Person.Name = "???? FAILED READ ????";
                                    user.Errors.Add(ex.Message);

                                    tmpResult.Add(user);
                                }

                                break;
                            }
                        }
                    }
                }
            });

            return result;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void ReadUsersUpdateProgress(List<User> tmpResult, List<User> result)
        {
            log.Debug("Read " + tmpResult.Count + " users. Total read " + result.Count + " users.");
            result.AddRange(tmpResult);
        }

        public List<OU> ReadOUHierarchy(string cvr, out List<FiltreretOejebliksbilledeType> allUnitRoles, Func<long, long, bool> progressCallback, ReadTasks readTasks = ReadTasks.YES, ReadManager readManager = ReadManager.YES, ReadAddresses readAddress = ReadAddresses.YES, ReadPayoutUnit readPayoutUnit = ReadPayoutUnit.YES, ReadContactPlaces readContactPlaces = ReadContactPlaces.YES, ReadPositions readPositions = ReadPositions.YES, ReadContactForTasks readContactForTasks = ReadContactForTasks.YES)
        {
            List<OU> result = new List<OU>();
            allUnitRoles = new List<FiltreretOejebliksbilledeType>();

            log.Info("Reading hiearchy: start");

            var registrations = new List<OrgUnitRegWrapper>();
            int offset = 0, hardstop = 0;
            while (true)
            {
                if (hardstop++ >= 20)
                {
                    log.Warn("Did 20 pages on object hierarchy, without seeing the end - aborting!");
                    break;
                }

                bool moreData = false;
                var res = organisationSystemStub.Read("500", "" + offset, out moreData);
                offset += 500;

                if (!moreData)
                {
                    break;
                }

                registrations.AddRange(res);
            }

            log.Info("Reading hiearchy: got all registrations: " + registrations.Count);

            // temporary, as out variables are not allowed in parallel below
            var someUnitRoles = new List<FiltreretOejebliksbilledeType>();

            // put ous into sets of 7 - so we can bulk operate on them
            var ousInBulk = new List<List<OrgUnitRegWrapper>>();
            var currentList = new List<OrgUnitRegWrapper>();
            ousInBulk.Add(currentList);

            int counter = 0;
            int total = 0;

            foreach (var registration in registrations)
            {
                currentList.Add(registration);
                counter++;
                total++;

                if (counter >= 7)
                {
                    currentList = new List<OrgUnitRegWrapper>();
                    ousInBulk.Add(currentList);
                    counter = 0;
                }
            }

            Parallel.ForEach(ousInBulk, new ParallelOptions { MaxDegreeOfParallelism = 4 }, (bulk) =>
            {
                // set cvr on thread
                OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

                int count = 0;

                // bit of logic to try reading each ou three times before giving up
                while (true)
                {
                    try
                    {
                        var mappedOus = MapRegistrationsToOUs(bulk, someUnitRoles, readTasks, readManager, readAddress, readPayoutUnit, readContactPlaces, readPositions, readContactForTasks);

                        AddToResult(mappedOus, result);

                        progressCallback?.Invoke(mappedOus.Count, registrations.Count);

                        count = 0;
                        break;
                    }
                    catch (Exception ex)
                    {
                        log.Warn("Timeout: " + ex.Message);
                        count++;
                        if (count >= 3)
                        {
                            throw;
                        }
                    }
                }
            });

            // copy to output
            allUnitRoles = someUnitRoles;

            log.Info("Reading hiearchy: got all units");

            return result;
        }

        private List<OU> MapRegistrationsToOUs(List<OrgUnitRegWrapper> wrappers, List<FiltreretOejebliksbilledeType> allUnitRoles, ReadTasks readTasks, ReadManager readManager, ReadAddresses readAddresses, ReadPayoutUnit readPayoutUnit, ReadContactPlaces readContactPlaces, ReadPositions readPositions, ReadContactForTasks readContactForTasks)
        {
            List<string> addressesToRead = new List<string>();
            var result = new List<OU>();

            foreach (var wrapper in wrappers)
            {
                var registration = wrapper.Registration;
                var uuid = wrapper.Uuid;
                DateTime timestamp = registration.Tidspunkt;
                List<string> errors = new List<string>();

                var property = StubUtil.GetLatestProperty(registration.AttributListe.Egenskab);
                string ouName = (property != null) ? property.EnhedNavn : null;
                string ouShortKey = (property != null) ? property.BrugervendtNoegleTekst : null;

                List<string> opgaver = new List<string>();
                if (readTasks.Equals(ReadTasks.YES) && registration.RelationListe?.Opgaver != null)
                {
                    foreach (var opgave in registration.RelationListe.Opgaver)
                    {
                        string task = opgave.ReferenceID?.Item;
                        if (!string.IsNullOrEmpty(task))
                        {
                            // TODO: consider converting to real KLE values instead of the UUIDs
                            opgaver.Add(task);
                        }
                    }
                }

                List<string> itSystemer = new List<string>();
                if (registration.RelationListe?.TilknyttedeItSystemer != null)
                {

                    foreach (var itSystem in registration.RelationListe.TilknyttedeItSystemer)
                    {
                        string itSystemUuid = itSystem.ReferenceID?.Item;
                        if (!string.IsNullOrEmpty(itSystemUuid))
                        {
                            itSystemer.Add(itSystemUuid);
                        }
                    }
                }

                List<AddressHolder> addresses = new List<AddressHolder>();
                if (readAddresses.Equals(ReadAddresses.YES) && registration.RelationListe?.Adresser != null)
                {
                    foreach (var address in registration.RelationListe.Adresser)
                    {
                        string addressUuid = address.ReferenceID.Item;
                        string addressIndex = address.Indeks;
                        addressesToRead.Add(addressUuid);

                        if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL))
                        {
                            addresses.Add(new Email()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOCATION))
                        {
                            addresses.Add(new Location()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE))
                        {
                            addresses.Add(new Phone()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSSHORTNAME))
                        {
                            addresses.Add(new LOSShortName()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSID))
                        {
                            addresses.Add(new LOSID()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_EAN))
                        {
                            addresses.Add(new Ean()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_URL))
                        {
                            addresses.Add(new Url()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LANDLINE))
                        {
                            addresses.Add(new Landline()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS))
                        {
                            addresses.Add(new ContactHours()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_DTR_ID))
                        {
                            addresses.Add(new DtrId()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL_REMARKS))
                        {
                            addresses.Add(new EmailRemarks()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST_RETURN))
                        {
                            addresses.Add(new PostReturn()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS))
                        {
                            addresses.Add(new Contact()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST))
                        {
                            addresses.Add(new Post()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE_OPEN_HOURS))
                        {
                            addresses.Add(new PhoneHours()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_FOA))
                        {
                            addresses.Add(new FOA()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_PNR))
                        {
                            addresses.Add(new PNR()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else if (address.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_SOR))
                        {
                            addresses.Add(new SOR()
                            {
                                Uuid = addressUuid,
                                AddressIndex = addressIndex
                            });
                        }
                        else
                        {
                            errors.Add("Address Rolle is unknown: " + address.Rolle.Item);
                        }
                    }
                }

                OUReference parentOU = null;
                if (registration.RelationListe?.Overordnet != null)
                {
                    string parentOUUuid = registration.RelationListe.Overordnet.ReferenceID.Item;
                    string parentOUName = "";

                    parentOU = new OUReference()
                    {
                        Name = parentOUName,
                        Uuid = parentOUUuid
                    };
                }

                List<string> contactForTasks = new List<string>();
                if (readContactForTasks.Equals(ReadContactForTasks.YES))
                {
                    contactForTasks = ServiceHelper.GetContactForTasks(uuid);
                }

                OUReference payoutOU = null;
                List<string> contactPlaces = new List<string>();
                if (readPayoutUnit.Equals(ReadPayoutUnit.YES) || readContactPlaces.Equals(ReadContactPlaces.YES))
                {
                    if (registration.RelationListe?.TilknyttedeFunktioner != null)
                    {
                        foreach (var function in registration.RelationListe.TilknyttedeFunktioner)
                        {
                            var functionState = orgFunctionStub.GetLatestRegistration(function.ReferenceID.Item);
                            if (functionState == null)
                            {
                                errors.Add("Referenced OrgFunktion does not exist: " + function.ReferenceID.Item);
                            }

                            if (functionState?.RelationListe?.Funktionstype != null)
                            {
                                if (readPayoutUnit.Equals(ReadPayoutUnit.YES) && functionState.RelationListe.Funktionstype.ReferenceID.Item.Equals(UUIDConstants.ORGFUN_PAYOUT_UNIT))
                                {
                                    if (functionState.RelationListe.TilknyttedeEnheder != null && functionState.RelationListe.TilknyttedeEnheder.Length > 0)
                                    {
                                        string payoutUnitUuid = functionState.RelationListe.TilknyttedeEnheder[0].ReferenceID.Item;
                                        string payoutUnitName = "The payout unit does not exist in Organisation";

                                        var payoutUnitRegistration = organisationEnhedStub.GetLatestRegistration(payoutUnitUuid);
                                        if (payoutUnitRegistration != null)
                                        {
                                            var payoutUnitProperty = StubUtil.GetLatestProperty(payoutUnitRegistration.AttributListe.Egenskab);

                                            if (payoutUnitProperty != null)
                                            {
                                                payoutUnitName = payoutUnitProperty.EnhedNavn;
                                            }
                                        }
                                        else
                                        {
                                            errors.Add("Referenced PayoutUnit does not exist: " + payoutUnitUuid);
                                        }

                                        payoutOU = new OUReference()
                                        {
                                            Name = payoutUnitName,
                                            Uuid = payoutUnitUuid
                                        };
                                    }
                                }
                                else if (readContactPlaces.Equals(ReadContactPlaces.YES) && functionState.RelationListe.Funktionstype.ReferenceID.Item.Equals(UUIDConstants.ORGFUN_CONTACT_UNIT))
                                {
                                    if (functionState.RelationListe.TilknyttedeEnheder != null && functionState.RelationListe.TilknyttedeEnheder.Length > 0)
                                    {
                                        string contactUnitUuid = functionState.RelationListe.TilknyttedeEnheder[0].ReferenceID.Item;

                                        contactPlaces.Add(contactUnitUuid);
                                    }
                                }
                            }
                        }
                    }
                }

                UserReference manager = new UserReference();
                if (readManager.Equals(ReadManager.YES))
                {
                    var managerRoles = ServiceHelper.FindManagerRolesForOrgUnitAsObjects(uuid);
                    if (managerRoles != null && managerRoles.Count > 0 && managerRoles[0].Registrering.Length > 0 && managerRoles[0].Registrering[0].RelationListe?.TilknyttedeBrugere != null && managerRoles[0].Registrering[0].RelationListe?.TilknyttedeBrugere.Length > 0)
                    {
                        manager.Uuid = managerRoles[0].Registrering[0].RelationListe?.TilknyttedeBrugere[0].ReferenceID?.Item;
                    }
                }

                List<Position> positions = new List<Position>();
                if (readPositions.Equals(ReadPositions.YES))
                {
                    ReadPositionsHandler(uuid, positions, errors, allUnitRoles);
                }

                OrgUnitType orgUnitType = OrgUnitType.DEPARTMENT;
                if (registration.RelationListe?.Enhedstype?.ReferenceID?.Item != null)
                {
                    if (UUIDConstants.ORGUNIT_TYPE_TEAM.Equals(registration.RelationListe.Enhedstype.ReferenceID.Item))
                    {
                        orgUnitType = OrgUnitType.TEAM;
                    }
                    else if (UUIDConstants.ORGUNIT_TYPE_DEPARTMENT.Equals(registration.RelationListe.Enhedstype.ReferenceID.Item))
                    {
                        orgUnitType = OrgUnitType.DEPARTMENT;
                    }
                    else
                    {
                        errors.Add("Unknown enhedstype: " + registration.RelationListe.Enhedstype.ReferenceID.Item);
                    }
                }

                Status status = Status.UNKNOWN;
                if (registration.TilstandListe.Gyldighed != null)
                {
                    var latestState = StubUtil.GetLatestGyldighed(registration.TilstandListe.Gyldighed);
                    if (latestState == null)
                    {
                        errors.Add("Object has no Tilstand!");
                        status = Status.UNKNOWN;
                    }
                    else if (global::IntegrationLayer.OrganisationEnhed.GyldighedStatusKodeType.Inaktiv.Equals(latestState.GyldighedStatusKode))
                    {
                        status = Status.INACTIVE;
                    }
                    else
                    {
                        status = Status.ACTIVE;
                    }
                }

                OU ou = new OU()
                {
                    Name = ouName,
                    ShortKey = ouShortKey,
                    Uuid = uuid,
                    ParentOU = parentOU,
                    Manager = manager,
                    Positions = positions,
                    PayoutOU = payoutOU,
                    Addresses = addresses,
                    Status = status,
                    Timestamp = timestamp,
                    Type = orgUnitType,
                    Tasks = opgaver,
                    ItSystems = itSystemer,
                    ContactForTasks = contactForTasks,
                    ContactPlaces = contactPlaces,
                    Errors = errors
                };

                if (!wrapper.Registration.LivscyklusKode.Equals(global::IntegrationLayer.OrganisationSystem.LivscyklusKodeType.Importeret) &&
                    !wrapper.Registration.LivscyklusKode.Equals(global::IntegrationLayer.OrganisationSystem.LivscyklusKodeType.Opstaaet))
                {
                    ou.Errors.Add("Has LivscyklusKode = " + wrapper.Registration.LivscyklusKode.ToString());
                }

                result.Add(ou);
            }

            // bulk read addresses
            if (readAddresses.Equals(ReadAddresses.YES) && addressesToRead.Count > 0)
            {
                var addressRegistrations = adresseStub.GetLatestRegistrations(addressesToRead);

                foreach (var addressUuid in addressRegistrations.Keys)
                {
                    var addressRegistration = addressRegistrations[addressUuid];

                    string adresseTekst = "";
                    string shortKey = "";
                    if (addressRegistration.AttributListe != null && addressRegistration.AttributListe.Count() > 0)
                    {
                        adresseTekst = addressRegistration.AttributListe[0].AdresseTekst;
                        shortKey = addressRegistration.AttributListe[0].BrugervendtNoegleTekst;
                    }

                    bool found = false;
                    foreach (var ou in result)
                    {
                        if (ou.Addresses != null)
                        {
                            foreach (var address in ou.Addresses)
                            {
                                if (address.Uuid.Equals(addressUuid))
                                {
                                    address.ShortKey = shortKey;
                                    address.Value = adresseTekst;

                                    found = true;
                                    break;
                                }
                            }
                        }

                        if (found)
                        {
                            break;
                        }
                    }
                }
            }

            return result;
        }

        private void ReadPositionsHandler(string uuid, List<Position> positions, List<string> errors, List<FiltreretOejebliksbilledeType> allUnitRoles)
        {
            var unitRoles = ServiceHelper.FindUnitRolesForOrgUnitAsObjects(uuid);

            lock (allUnitRolesLock)
            {
                if (allUnitRoles != null)
                {
                    allUnitRoles.AddRange(unitRoles);
                }
            }

            foreach (var unitRole in unitRoles)
            {
                RegistreringType1 orgFunctionRegistration = null;
                if (unitRole.Registrering != null && unitRole.Registrering.Length > 0)
                {
                    orgFunctionRegistration = unitRole.Registrering[0];
                }

                string orgFunctionName = "OrgFunction object does not exist in Organisation";
                string orgFunctionShortKey = "OrgFunction object does not exist in Organisation";

                if (orgFunctionRegistration != null)
                {
                    var orgFunctionProperty = StubUtil.GetLatestProperty(orgFunctionRegistration.AttributListe.Egenskab);

                    if (orgFunctionProperty != null)
                    {
                        orgFunctionName = orgFunctionProperty.FunktionNavn;
                        orgFunctionShortKey = orgFunctionProperty.BrugervendtNoegleTekst;
                    }

                    if (orgFunctionRegistration.RelationListe.TilknyttedeBrugere != null && orgFunctionRegistration.RelationListe.TilknyttedeBrugere.Length > 0)
                    {
                        // the registration pattern allows for multiple users to share an OrgFunction
                        foreach (var bruger in orgFunctionRegistration.RelationListe.TilknyttedeBrugere)
                        {
                            UserReference user = new UserReference()
                            {
                                Uuid = bruger.ReferenceID.Item
                            };

                            Position position = new Position()
                            {
                                Name = orgFunctionName,
                                User = user,
                                ShortKey = orgFunctionShortKey,
                                Uuid = unitRole.ObjektType.UUIDIdentifikator
                            };

                            positions.Add(position);
                        }
                    }
                }
                else
                {
                    if (errors != null)
                    {
                        errors.Add("OrganisationFunktion does not contain any registrations: " + unitRole.ObjektType.UUIDIdentifikator);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void AddToResult(List<OU> ous, List<OU> result)
        {
            result.AddRange(ous);
        }
    }
}
