using System;
using System.Collections.Generic;
using Organisation.IntegrationLayer;
using Organisation.BusinessLayer.DTO.Registration;
using static Organisation.BusinessLayer.DTO.Registration.OrgUnitRegistration;

namespace Organisation.BusinessLayer
{
   public class OrgUnitService
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationEnhedStub organisationEnhedStub = new OrganisationEnhedStub();
        private OrganisationFunktionStub organisationFunktionStub = new OrganisationFunktionStub();
        private OrganisationStub organisationStub = new OrganisationStub();
        private InspectorService inspectorService = new InspectorService();

        /// <summary>
        /// This method will create the object in Organisation - note that if the object already exists, this method
        /// will fail. If unsure whether the object exists, use Update() instead, as that will fallback to Create
        /// if the object does not exist.
        /// </summary>
        public void Create(OrgUnitRegistration registration)
        {
            log.Debug("Performing Create on OrgUnit '" + registration.Uuid + "'");

            ValidateAndEnforceCasing(registration);

            try
            {
                var addressRefs = ImportAddresses(registration);

                // mapping the unit must come after the addresses, as importing the address might set a UUID on the addresses if not supplied by the caller
                OrgUnitData orgUnitData = MapRegistrationToOrgUnitDTO(registration, addressRefs);

                // create manager relationship
                if (!string.IsNullOrEmpty(registration.ManagerUuid))
                {
                    ServiceHelper.UpdateManager(registration);
                }

                // if this unit is a working unit, that does payouts in behalf of a payout unit, create a reference to that payout unit
                if (!string.IsNullOrEmpty(registration.PayoutUnitUuid))
                {
                    string payoutUnitFunctionUuid = ServiceHelper.EnsurePayoutUnitFunctionExists(registration.PayoutUnitUuid, registration.Timestamp);

                    orgUnitData.OrgFunctionUuids.Add(payoutUnitFunctionUuid);
                }

                organisationEnhedStub.Importer(orgUnitData);

                UpdateOrganisationObject(orgUnitData);

                // ensure "henvendelsessted" tasks are created
                ServiceHelper.UpdateContactForTasks(registration.Uuid, registration.ContactForTasks, registration.Timestamp);

                log.Debug("Create successful on OrgUnit '" + registration.Uuid + "'");
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Create on OrgUnitService failed for '" + registration.Uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        /// <summary>
        /// This method will perform a soft-delete on the given OrgUnit. As objects are never really deleted within Organisation,
        /// it means that the object will be orphaned in the sense that all direct and indirect relationships to and from the municipalities
        /// organisation object will be severed.
        /// </summary>
        public void Delete(string uuid, DateTime timestamp)
        {
            try
            {
                log.Debug("Performing Delete on OrgUnit '" + uuid + "'");

                // TODO: should we also find ContactPlace functions and terminate them, as they are not shared with anyone else (lifecycle should be tied to this object)
                //       when updating how objects are terminated (KOMBIT pending change), look into this...

                // drop all relationsships from the OU to anything else
                organisationEnhedStub.Deactivate(uuid, timestamp);

                // ensure "henvendelsessted" tasks are removed
                ServiceHelper.DeleteContactForTasks(uuid, timestamp);

                log.Debug("Delete successful on OrgUnit '" + uuid + "'");
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Delete on OrgUnitService failed for '" + uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        /// <summary>
        /// This method will check whether the OrgUnit already exists inside Organisation, read it if it does, and perform
        /// the correct update (registering the delta-changes between the local object and the org-object). If the object
        /// does not already exist, it will pass the registration to the Create method.
        /// </summary>
        public void Update(OrgUnitRegistration registration)
        {
            log.Debug("Performing Update on OrgUnit '" + registration.Uuid + "'");

            ValidateAndEnforceCasing(registration);

            try
            {
                var result = organisationEnhedStub.GetLatestRegistration(registration.Uuid);
                if (result == null)
                {
                    log.Debug("Update on OrgUnit '" + registration.Uuid + "' changed to a Create because it does not exists as an active object within Organisation");
                    Create(registration);
                }
                else
                {
                    var addressRefs = UpdateAddresses(registration, result);

                    // this must happen after addresses have been imported, as it might result in UUID's being created
                    OrgUnitData orgUnitData = MapRegistrationToOrgUnitDTO(registration, addressRefs);

                    #region Update payout units
                    // if this unit handles payouts on behalf of a payout unit, create a reference to that payout unit
                    if (!string.IsNullOrEmpty(registration.PayoutUnitUuid))
                    {
                        string payoutUnitFunctionUuid = ServiceHelper.EnsurePayoutUnitFunctionExists(registration.PayoutUnitUuid, registration.Timestamp);

                        orgUnitData.OrgFunctionUuids.Add(payoutUnitFunctionUuid);
                    }
                    #endregion

                    ServiceHelper.UpdateManager(registration);

                    organisationEnhedStub.Ret(orgUnitData);

                    // ensure "henvendelsessted" tasks are updated
                    ServiceHelper.UpdateContactForTasks(registration.Uuid, registration.ContactForTasks, registration.Timestamp);

                    UpdateOrganisationObject(orgUnitData);

                    log.Debug("Update successful on OrgUnit '" + registration.Uuid + "'");
                }
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Update on OrgUnitService failed for '" + registration.Uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        private List<AddressRelation> UpdateAddresses(OrgUnitRegistration registration, global::IntegrationLayer.OrganisationEnhed.RegistreringType1 result)
        {
            // check what already exists in Organisation - and store the UUIDs of the existing addresses, we will need those later
            string orgPhoneUuid = null, orgEmailUuid = null, orgLocationUuid = null, orgLOSShortNameUuid = null, orgEanUuid = null, orgContactHoursUuid = null, orgPhoneHoursUuid = null, orgPostUuid = null, orgPostReturnUuid = null, orgContactUuid = null, orgEmailRemarksUuid = null, orgLandlineUuid = null, orgUrlUuid = null;

            if (result.RelationListe.Adresser != null)
            {
                foreach (var orgAddress in result.RelationListe.Adresser)
                {
                    if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE))
                    {
                        orgPhoneUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL))
                    {
                        orgEmailUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOCATION))
                    {
                        orgLocationUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSSHORTNAME))
                    {
                        orgLOSShortNameUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_URL))
                    {
                        orgUrlUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LANDLINE))
                    {
                        orgLandlineUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_EAN))
                    {
                        orgEanUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_PHONE_OPEN_HOURS))
                    {
                        orgPhoneHoursUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST))
                    {
                        orgPostUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS))
                    {
                        orgContactHoursUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_POST_RETURN))
                    {
                        orgPostReturnUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS))
                    {
                        orgContactUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_EMAIL_REMARKS))
                    {
                        orgEmailRemarksUuid = orgAddress.ReferenceID.Item;
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

            ServiceHelper.UpdateAddress(registration.Email, orgEmailUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.EMAIL
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

            ServiceHelper.UpdateAddress(registration.LOSShortName, orgLOSShortNameUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.LOSSHORTNAME
                });
            }

            ServiceHelper.UpdateAddress(registration.Ean, orgEanUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.EAN
                });
            }

            ServiceHelper.UpdateAddress(registration.Url, orgUrlUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.URL
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

            ServiceHelper.UpdateAddress(registration.ContactOpenHours, orgContactHoursUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.CONTACT_ADDRESS_OPEN_HOURS
                });
            }

            ServiceHelper.UpdateAddress(registration.EmailRemarks, orgEmailRemarksUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.EMAIL_REMARKS
                });
            }

            ServiceHelper.UpdateAddress(registration.PostReturn, orgPostReturnUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.POST_RETURN
                });
            }

            ServiceHelper.UpdateAddress(registration.Contact, orgContactUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.CONTACT_ADDRESS
                });
            }

            ServiceHelper.UpdateAddress(registration.PhoneOpenHours, orgPhoneHoursUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.PHONE_OPEN_HOURS
                });
            }
            
            ServiceHelper.UpdateAddress(registration.Post, orgPostUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.POST
                });
            }

            return addressRefs;
        }

        private List<AddressRelation> ImportAddresses(OrgUnitRegistration registration)
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

            if (!string.IsNullOrEmpty(registration.LOSShortName))
            {
                ServiceHelper.ImportAddress(registration.LOSShortName, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.LOSSHORTNAME
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.EmailRemarks))
            {
                ServiceHelper.ImportAddress(registration.EmailRemarks, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.EMAIL_REMARKS
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.PostReturn))
            {
                ServiceHelper.ImportAddress(registration.PostReturn, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.POST_RETURN
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.Contact))
            {
                ServiceHelper.ImportAddress(registration.Contact, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.CONTACT_ADDRESS
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.PhoneOpenHours))
            {
                ServiceHelper.ImportAddress(registration.PhoneOpenHours, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.PHONE_OPEN_HOURS
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.ContactOpenHours))
            {
                ServiceHelper.ImportAddress(registration.ContactOpenHours, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.CONTACT_ADDRESS_OPEN_HOURS
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.Ean))
            {
                ServiceHelper.ImportAddress(registration.Ean, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.EAN
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.Url))
            {
                ServiceHelper.ImportAddress(registration.Url, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.URL
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

            if (!string.IsNullOrEmpty(registration.Post))
            {
                ServiceHelper.ImportAddress(registration.Post, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.POST
                    });
                }
            }

            return addressRefs;
        }

        private void UpdateOrganisationObject(OrgUnitData orgUnitData)
        {
            try
            {
                // if this is the root, we need to update the Organisation object
                if (string.IsNullOrEmpty(orgUnitData.ParentOrgUnitUuid))
                {
                    organisationStub.Ret(orgUnitData.Uuid);
                }
            }
            catch (Exception ex)
            {
                // this is okay - it is expected that KOMBIT will take ownership of the object in the future
                log.Warn("Failed to update Organisation object with Overordnet relationship - probably because of KOMBIT ownership: " + ex.Message);
            }
        }

        public List<string> List()
        {
            log.Debug("Performing List on OrgUnits");

            var result = inspectorService.FindAllOUs();

            log.Debug("Found " + result.Count + " OrgUnits");

            return result;
        }

        public OrgUnitRegistration Read(string uuid)
        {
            log.Debug("Performing Read on OrgUnit " + uuid);

            OrgUnitRegistration registration = null;

            var ou = inspectorService.ReadOUObject(uuid, ReadTasks.YES, ReadManager.YES, ReadAddresses.YES, ReadPayoutUnit.YES, ReadPositions.NO, ReadContactForTasks.YES);
            if (ou != null)
            {
                registration = new OrgUnitRegistration();

                registration.Name = ou.Name;
                registration.ParentOrgUnitUuid = ou.ParentOU?.Uuid;
                registration.PayoutUnitUuid = ou.PayoutOU?.Uuid;
                registration.ShortKey = ou.ShortKey;
                registration.Type = ou.Type;
                registration.Uuid = uuid;
                registration.ManagerUuid = ou.Manager.Uuid;
                registration.Tasks = ou.Tasks;
                registration.ContactForTasks = ou.ContactForTasks;

                foreach (var address in ou.Addresses)
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
                    else if (address is DTO.Read.LOSShortName)
                    {
                        registration.LOSShortName = address.Value;
                    }
                    else if (address is DTO.Read.PostReturn)
                    {
                        registration.PostReturn = address.Value;
                    }
                    else if (address is DTO.Read.Contact)
                    {
                        registration.Contact = address.Value;
                    }
                    else if (address is DTO.Read.EmailRemarks)
                    {
                        registration.EmailRemarks = address.Value;
                    }
                    else if (address is DTO.Read.PhoneHours)
                    {
                        registration.PhoneOpenHours = address.Value;
                    }
                    else if (address is DTO.Read.ContactHours)
                    {
                        registration.ContactOpenHours = address.Value;
                    }
                    else if (address is DTO.Read.Url)
                    {
                        registration.Url = address.Value;
                    }
                    else if (address is DTO.Read.Landline)
                    {
                        registration.Landline = address.Value;
                    }
                    else if (address is DTO.Read.Ean)
                    {
                        registration.Ean = address.Value;
                    }
                    else if (address is DTO.Read.Post)
                    {
                        registration.Post = address.Value;
                    }
                    else
                    {
                        log.Warn("Trying to Read OrgUnit " + uuid + " with unknown address type " + address.GetType().ToString());
                    }
                }

                registration.Timestamp = ou.Timestamp;

                log.Debug("Found OrgUnit " + uuid + " when reading");
            }
            else
            {
                log.Debug("Did not find OrgUnit " + uuid + " when reading");
            }

            return registration;
        }

        private OrgUnitData MapRegistrationToOrgUnitDTO(OrgUnitRegistration registration, List<AddressRelation> addressRefs)
        {
            OrgUnitData organisationEnhed = new OrgUnitData();
            organisationEnhed.ShortKey = registration.ShortKey;
            organisationEnhed.Name = registration.Name;
            organisationEnhed.Addresses = addressRefs;
            organisationEnhed.Timestamp = registration.Timestamp;
            organisationEnhed.Uuid = registration.Uuid;
            organisationEnhed.ParentOrgUnitUuid = registration.ParentOrgUnitUuid;
            organisationEnhed.Tasks = registration.Tasks;

            switch (registration.Type)
            {
                case OrgUnitType.DEPARTMENT:
                    organisationEnhed.OrgUnitType = UUIDConstants.ORGUNIT_TYPE_DEPARTMENT;
                    break;
                case OrgUnitType.TEAM:
                    organisationEnhed.OrgUnitType = UUIDConstants.ORGUNIT_TYPE_TEAM;
                    break;
                default:
                    throw new Exception("Unknown type: " + registration.Type);
            }

            return organisationEnhed;
        }

        private void ValidateAndEnforceCasing(OrgUnitRegistration registration)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(registration.Name))
            {
                errors.Add("name");
            }

            if (string.IsNullOrEmpty(registration.Uuid))
            {
                errors.Add("uuid");
            }

            if (registration.Timestamp == null)
            {
                errors.Add("timestamp");
            }

            if (errors.Count > 0)
            {
                throw new InvalidFieldsException("Invalid registration object - the following fields are invalid: " + string.Join(",", errors));
            }

            registration.Uuid = registration.Uuid.ToLower();
        }
    }
}