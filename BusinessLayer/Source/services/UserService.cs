using System;
using System.Collections.Generic;
using Organisation.IntegrationLayer;
using Organisation.BusinessLayer.DTO.Registration;
using IntegrationLayer.OrganisationFunktion;

namespace Organisation.BusinessLayer
{
    public class UserService
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private BrugerStub brugerStub = new BrugerStub();
        private OrganisationFunktionStub orgFunctionStub = new OrganisationFunktionStub();
        private InspectorService inspectorService = new InspectorService();

        public void Create(UserRegistration user)
        {
            log.Debug("Performing Create on User '" + user.Uuid + "'");

            ValidateAndEnforceCasing(user);

            try
            {
                var addressRefs = ImportAddresses(user);

                // create new person object
                string personUuid = null;
                ServiceHelper.UpdatePerson(user, null, out personUuid);

                // create the position
                ServiceHelper.UpdatePosition(user);

                // create User object
                brugerStub.Importer(MapRegistrationToUserDTO(user, addressRefs, personUuid));

                log.Debug("Create successful on User '" + user.Uuid + "'");
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Create on UserService failed for '" + user.Uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        public void Update(UserRegistration user)
        {
            log.Debug("Performing Update on User '" + user.Uuid + "'");

            ValidateAndEnforceCasing(user);

            try
            {
                var result = brugerStub.GetLatestRegistration(user.Uuid);
                if (result == null)
                {
                    log.Debug("Update on User '" + user.Uuid + "' changed to a Create because it does not exists as an active object within Organisation");
                    Create(user);
                }
                else
                {
                    // wipe all existing addresses if needed
                    if (OrganisationRegistryProperties.AppSettings.RecreateBrugerAddresses)
                    {
                        // terminate all Address relationships
                        brugerStub.WipeAddresses(user.Uuid, user.Timestamp);

                        // reload to re-add addresses :)
                        result = brugerStub.GetLatestRegistration(user.Uuid);
                    }

                    var addressRefs = UpdateAddresses(user, result);

                    ServiceHelper.UpdatePosition(user);

                    string orgPersonUuid = null;
                    if (result.RelationListe.TilknyttedePersoner != null && result.RelationListe.TilknyttedePersoner.Length > 0)
                    {
                        // we read actual-state-only, so there is precisely ONE person object - but better safe than sorry
                        orgPersonUuid = result.RelationListe.TilknyttedePersoner[0].ReferenceID.Item;
                    }

                    string personUuid = null;
                    ServiceHelper.UpdatePerson(user, orgPersonUuid, out personUuid);

                    // Update the User object (attributes and all relationships)
                    brugerStub.Ret(MapRegistrationToUserDTO(user, addressRefs, personUuid));

                    log.Debug("Update successful on User '" + user.Uuid + "'");
                }
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Update on UserService failed for '" + user.Uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        public void Passiver(string uuid)
        {
            try
            {
                brugerStub.Passiver(uuid);
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Passiver on UserService failed for '" + uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        public void Delete(string uuid, DateTime timestamp)
        {
            try
            {
                // find the OrgFunctions that represents this users positions within the municipality
                // for each of these OrgFunctions and drop the relationship to both User and OrgUnit from that Function
                List<FiltreretOejebliksbilledeType> unitRoles = ServiceHelper.FindUnitRolesForUser(uuid);
                foreach (FiltreretOejebliksbilledeType unitRole in unitRoles)
                {
                    orgFunctionStub.Deactivate(unitRole.ObjektType.UUIDIdentifikator, timestamp);
                }

                // update the user object by
                //   -> terminating the users relationship to the Organisation
                brugerStub.Deactivate(uuid, timestamp);
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Delete on UserService failed for '" + uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        public List<string> List()
        {
            log.Debug("Performing List on Users");

            var result = inspectorService.FindAllUsers();

            log.Debug("Found " + result.Count + " Users");

            return result;
        }

        public UserRegistration Read(string uuid)
        {
            log.Debug("Performing Read on User " + uuid);

            UserRegistration registration = null;

            var user = inspectorService.ReadUserObject(uuid, ReadAddresses.YES, ReadParentDetails.NO);
            if (user != null)
            {
                registration = new UserRegistration();
                registration.Uuid = uuid;
                registration.UserId = user.UserId;
                registration.ShortKey = user.ShortKey;
                registration.IsRobot = user.IsRobot;

                if (user.Person != null)
                {
                    registration.Person = new Person()
                    {
                        Cpr = user.Person.Cpr,
                        Name = user.Person.Name,
                        Uuid = user.Person.Uuid
                    };
                }

                registration.Status = user.Status;

                foreach (var position in user.Positions)
                {
                    Position userPosition = new Position();
                    userPosition.Name = position.Name;
                    userPosition.OrgUnitUuid = position.OU.Uuid;
                    userPosition.StartDate = position.StartDate;
                    userPosition.StopDate = position.StopDate;

                    registration.Positions.Add(userPosition);
                }

                foreach (var address in user.Addresses)
                {
                    if (address is DTO.Read.Email)
                    {
                        registration.Email = address.Value;
                    }
                    else if (address is DTO.Read.Location)
                    {
                        registration.Location = address.Value;
                    }
                    else if (address is DTO.Read.Phone)
                    {
                        registration.PhoneNumber = address.Value;
                    }
                    else if (address is DTO.Read.Landline)
                    {
                        registration.Landline = address.Value;
                    }
                    else if (address is DTO.Read.RacfID)
                    {
                        registration.RacfID = address.Value;
                    }
                    else if (address is DTO.Read.FMKID)
                    {
                        registration.FMKID = address.Value;
                    }
                    else
                    {
                        log.Warn("Trying to Read user " + uuid + " with unknown address type " + address.GetType().ToString());
                    }
                }

                log.Debug("Found User " + uuid + " when reading");

                registration.Timestamp = user.Timestamp;
            }
            else
            {
                log.Debug("Did not found User " + uuid + " when reading");
            }

            return registration;
        }

        private List<AddressRelation> ImportAddresses(UserRegistration registration)
        {
            var addressRefs = new List<AddressRelation>();
            string uuid;

            if (!string.IsNullOrEmpty(registration.PhoneNumber))
            {
                ServiceHelper.ImportAddress(registration.PhoneNumber, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.PHONE
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.Landline))
            {
                ServiceHelper.ImportAddress(registration.Landline, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.LANDLINE
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.Email))
            {
                ServiceHelper.ImportAddress(registration.Email, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.EMAIL
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.RacfID))
            {
                ServiceHelper.ImportAddress(registration.RacfID, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.RACFID
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.Location))
            {
                ServiceHelper.ImportAddress(registration.Location, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.LOCATION
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.FMKID))
            {
                ServiceHelper.ImportAddress(registration.FMKID, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.FMKID
                    });
                }
            }

            return addressRefs;
        }

        private List<AddressRelation> UpdateAddresses(UserRegistration registration, global::IntegrationLayer.Bruger.RegistreringType1 result)
        {
            // check what already exists in Organisation - and store the UUIDs of the existing addresses, we will need those later
            string orgPhoneUuid = null, orgEmailUuid = null, orgLocationUuid = null, orgRacfIDUuid = null, orgLandlineUuid = null, orgFMKIDUuid = null;

            if (result.RelationListe.Adresser != null)
            {
                foreach (var orgAddress in result.RelationListe.Adresser)
                {
                    if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_PHONE))
                    {
                        orgPhoneUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_LANDLINE))
                    {
                        orgLandlineUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_EMAIL))
                    {
                        orgEmailUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_LOCATION))
                    {
                        orgLocationUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_RACFID))
                    {
                        orgRacfIDUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_USER_FMKID))
                    {
                        orgFMKIDUuid = orgAddress.ReferenceID.Item;
                    }
                }
            }

            // run through all the input addresses, and deal with them one by one
            List<AddressRelation> addressRefs = new List<AddressRelation>();
            string uuid;

            ServiceHelper.UpdateAddress(registration.PhoneNumber, orgPhoneUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.PHONE
                });
            }

            ServiceHelper.UpdateAddress(registration.Landline, orgLandlineUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.LANDLINE
                });
            }

            ServiceHelper.UpdateAddress(registration.Email, orgEmailUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.EMAIL
                });
            }

            ServiceHelper.UpdateAddress(registration.RacfID, orgRacfIDUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.RACFID
                });
            }

            ServiceHelper.UpdateAddress(registration.Location, orgLocationUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.LOCATION
                });
            }

            ServiceHelper.UpdateAddress(registration.FMKID, orgFMKIDUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.FMKID
                });
            }

            return addressRefs;
        }

        private UserData MapRegistrationToUserDTO(UserRegistration registration, List<AddressRelation> addressRefs, string personUuid)
        {
            UserData user = new UserData();
            user.Addresses = addressRefs;
            user.ShortKey = registration.ShortKey;
            user.Timestamp = registration.Timestamp;
            user.UserId = registration.UserId;
            user.Uuid = registration.Uuid;
            user.PersonUuid = personUuid;
            user.IsRobot = registration.IsRobot;

            return user;
        }

        private void ValidateAndEnforceCasing(UserRegistration registration)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(registration.Person.Name))
            {
                errors.Add("personName");
            }

            foreach (Position position in registration.Positions)
            {
                if (string.IsNullOrEmpty(position.Name))
                {
                    errors.Add("positionName");
                }

                if (string.IsNullOrEmpty(position.OrgUnitUuid))
                {
                    errors.Add("positionOrgUnitUuid");
                }
            }

            if (string.IsNullOrEmpty(registration.UserId))
            {
                errors.Add("userId");
            }

            if (string.IsNullOrEmpty(registration.Uuid))
            {
                errors.Add("userUuid");
            }

            if (errors.Count > 0)
            {
                throw new InvalidFieldsException("Invalid registration object - the following fields are invalid: " + string.Join(",", errors));
            }

            foreach (Position position in registration.Positions)
            {
                position.OrgUnitUuid = position.OrgUnitUuid.ToLower();
            }

            if (registration.Person.Cpr != null)
            {
                // strip dashes, so 010101-0101 becomes 010101010101 (KOMBIT requirement)
                registration.Person.Cpr = registration.Person.Cpr.Replace("-", "");
            }

            registration.Uuid = registration.Uuid.ToLower();
        }
    }
}
