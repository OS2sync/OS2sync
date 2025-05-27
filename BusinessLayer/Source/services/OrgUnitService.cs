using System;
using System.Collections.Generic;
using Organisation.IntegrationLayer;
using Organisation.BusinessLayer.DTO.Registration;
using static Organisation.BusinessLayer.DTO.Registration.OrgUnitRegistration;
using Organisation.BusinessLayer.DTO.Read;
using IntegrationLayer.OrganisationEnhed;

namespace Organisation.BusinessLayer
{
   public class OrgUnitService
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrganisationEnhedStub organisationEnhedStub = new OrganisationEnhedStub();
        private OrganisationStub organisationStub = new OrganisationStub();
        private OrganisationFunktionStub organisationFunktionStub = new OrganisationFunktionStub();
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

                bool skipUdbetalingsenheder = DisableUdbetalingsenheder();
                bool skipHenvendelsessteder = DisableHenvendelsessteder();

                // if this unit is a working unit, that does payouts in behalf of a payout unit, create a reference to that payout unit
                if (!string.IsNullOrEmpty(registration.PayoutUnitUuid))
                {
                    if (!skipUdbetalingsenheder)
                    {
                        string payoutUnitFunctionUuid = ServiceHelper.EnsurePayoutUnitFunctionExists(registration.PayoutUnitUuid, registration.Timestamp);
                        orgUnitData.OrgFunctionsToAdd.Add(payoutUnitFunctionUuid);
                    }
                }

                // if this unit uses contactPlaces, make sure to add them
                if (registration.ContactPlaces != null && registration.ContactPlaces.Count > 0)
                {
                    if (!skipHenvendelsessteder)
                    {
                        foreach (var cp in registration.ContactPlaces)
                        {
                            string functionUuid = ServiceHelper.GetContactPlaceFunctionUuid(cp);
                            if (functionUuid != null)
                            {
                                orgUnitData.OrgFunctionsToAdd.Add(functionUuid);
                            }
                        }
                    }
                }

                organisationEnhedStub.Importer(orgUnitData);

                UpdateOrganisationObject(orgUnitData);

                // ensure "henvendelsessted" tasks are created
                if (!skipHenvendelsessteder)
                {
                    ServiceHelper.UpdateContactForTasks(registration.Uuid, registration.ContactForTasks, registration.Timestamp);
                }

                log.Debug("Create successful on OrgUnit '" + registration.Uuid + "'");
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Create on OrgUnitService failed for '" + registration.Uuid + "' due to unavailable KOMBIT services", ex);
                throw new TemporaryFailureException(ex.Message);
            }
        }

        public void Passiver(string uuid)
        {
            try
            {
                organisationEnhedStub.Passiver(uuid);
            }
            catch (Exception ex) when (ex is STSNotFoundException || ex is ServiceNotFoundException)
            {
                log.Warn("Passiver on OrgUnitService failed for '" + uuid + "' due to unavailable KOMBIT services", ex);
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
                    // wipe all existing addresses if needed
                    if (OrganisationRegistryProperties.AppSettings.RecreateOrgunitAddresses)
                    {
                        // terminate all Address relationships
                        organisationEnhedStub.WipeAddresses(registration.Uuid, registration.Timestamp);

                        // reload to re-add addresses :)
                        result = organisationEnhedStub.GetLatestRegistration(registration.Uuid);
                    }

                    var addressRefs = UpdateAddresses(registration, result);

                    // this must happen after addresses have been imported, as it might result in UUID's being created
                    OrgUnitData orgUnitData = MapRegistrationToOrgUnitDTO(registration, addressRefs);

                    // deal with ContactPlaces and PayoutUnits
                    if (!DisableHenvendelsessteder() || !DisableUdbetalingsenheder())
                    {
                        // read all existing functions (if any)
                        List<string> existingFunctionUuids = new List<string>();
                        if (result.RelationListe.TilknyttedeFunktioner != null)
                        {
                            foreach (var tf in result.RelationListe.TilknyttedeFunktioner)
                            {
                                if (tf.ReferenceID?.Item != null)
                                {
                                    existingFunctionUuids.Add(tf.ReferenceID.Item);
                                }
                            }
                        }

                        global::IntegrationLayer.OrganisationFunktion.FiltreretOejebliksbilledeType[] existingFunctionDetails = new global::IntegrationLayer.OrganisationFunktion.FiltreretOejebliksbilledeType[0];
                        if (existingFunctionUuids.Count > 0)
                        {
                            existingFunctionDetails = organisationFunktionStub.GetLatestRegistrations(existingFunctionUuids.ToArray());
                        }

                        #region Update ContactPlaces
                        if (!DisableHenvendelsessteder())
                        {
                            List<string> contactPlaceFunctionUuids = new List<string>();

                            if (registration.ContactPlaces != null && registration.ContactPlaces.Count > 0)
                            {
                                foreach (var cp in registration.ContactPlaces)
                                {
                                    string functionUuid = ServiceHelper.GetContactPlaceFunctionUuid(cp);
                                    if (functionUuid != null)
                                    {
                                        orgUnitData.OrgFunctionsToAdd.Add(functionUuid);
                                        contactPlaceFunctionUuids.Add(functionUuid);
                                    }
                                    else
                                    {
                                        log.Warn("OrgUnit " + registration.Uuid + " points to a ContactPlace (" + cp + ") that does not have a ContactPlace OrgunitFunction for it");
                                    }
                                }
                            }

                            // and which ones should we remove?
                            foreach (var fot in existingFunctionDetails)
                            {
                                if (fot.Registrering == null || fot.Registrering.Length == 0)
                                {
                                    continue;
                                }

                                string functionTypeUuid = fot.Registrering[0].RelationListe.Funktionstype?.ReferenceID?.Item;
                                if (!UUIDConstants.ORGFUN_CONTACT_UNIT.Equals(functionTypeUuid))
                                {
                                    continue;
                                }

                                bool found = false;
                                foreach (var cpFunUuid in contactPlaceFunctionUuids)
                                {
                                    if (cpFunUuid.Equals(fot.ObjektType.UUIDIdentifikator))
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    orgUnitData.OrgFunctionsToRemove.Add(fot.ObjektType.UUIDIdentifikator);
                                }
                            }
                        }
                        #endregion

                        #region Update payout units
                        if (!DisableUdbetalingsenheder())
                        {
                            string payoutUnitFunctionUuid = null;

                            // if this unit handles payouts on behalf of a payout unit, create a reference to that payout unit
                            if (!string.IsNullOrEmpty(registration.PayoutUnitUuid))
                            {
                                payoutUnitFunctionUuid = ServiceHelper.EnsurePayoutUnitFunctionExists(registration.PayoutUnitUuid, registration.Timestamp);

                                orgUnitData.OrgFunctionsToAdd.Add(payoutUnitFunctionUuid);
                            }

                            // cleanup any other
                            foreach (var fot in existingFunctionDetails)
                            {
                                if (fot.Registrering == null || fot.Registrering.Length == 0)
                                {
                                    continue;
                                }

                                string functionTypeUuid = fot.Registrering[0].RelationListe.Funktionstype?.ReferenceID?.Item;
                                if (!UUIDConstants.ORGFUN_PAYOUT_UNIT.Equals(functionTypeUuid))
                                {
                                    continue;
                                }

                                if (payoutUnitFunctionUuid == null || !payoutUnitFunctionUuid.Equals(fot.ObjektType.UUIDIdentifikator))
                                {
                                    orgUnitData.OrgFunctionsToRemove.Add(fot.ObjektType.UUIDIdentifikator);
                                }
                            }
                        }
                        #endregion
                    }

                    ServiceHelper.UpdateManager(registration);

                    organisationEnhedStub.Ret(orgUnitData);

                    // ensure "henvendelsessted" tasks are updated
                    if (!DisableHenvendelsessteder())
                    {
                        ServiceHelper.UpdateContactForTasks(registration.Uuid, registration.ContactForTasks, registration.Timestamp);
                    }

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
            string orgPhoneUuid = null, orgEmailUuid = null, orgLocationUuid = null, orgDtrIdUuid = null, orgLOSShortNameUuid = null, orgEanUuid = null, orgContactHoursUuid = null, orgPhoneHoursUuid = null, orgPostUuid = null, orgPostSecondaryUuid = null, orgPostReturnUuid = null, orgContactUuid = null, orgEmailRemarksUuid = null, orgLandlineUuid = null, orgUrlUuid = null, orgLosIdUuid = null, orgFOAUuid = null, orgPNRUuid = null, orgSORUuid = null;

            if (result.RelationListe.Adresser != null)
            {
                var posts = new List<AdresseFlerRelationType>();
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
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_LOSID))
                    {
                        orgLosIdUuid = orgAddress.ReferenceID.Item;
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
                        posts.Add(orgAddress);
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_CONTACT_ADDRESS_OPEN_HOURS))
                    {
                        orgContactHoursUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_DTR_ID))
                    {
                        orgDtrIdUuid = orgAddress.ReferenceID.Item;
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
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_FOA))
                    {
                        orgFOAUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_PNR))
                    {
                        orgPNRUuid = orgAddress.ReferenceID.Item;
                    }
                    else if (orgAddress.Rolle.Item.Equals(UUIDConstants.ADDRESS_ROLE_ORGUNIT_SOR))
                    {
                        orgSORUuid = orgAddress.ReferenceID.Item;
                    }
                }

                // as we support 2 post addresses, we pick the two with the higest Index ;)
                posts.Sort((x, y) => x.Indeks.CompareTo(y.Indeks));
                foreach (var post in posts)
                {
                    if (orgPostUuid == null)
                    {
                        orgPostUuid = post.ReferenceID.Item;
                    }
                    else if (orgPostSecondaryUuid == null)
                    {
                        orgPostSecondaryUuid = post.ReferenceID.Item;
                    }
                }
            }

            // run through all the input addresses, and deal with them one by one
            List<AddressRelation> addressRefs = new List<AddressRelation>();
            string uuid;

            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("PHONE"))
            {
                ServiceHelper.UpdateAddress(registration.PhoneNumber, orgPhoneUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.PHONE
                    });
                }
            }
            else if (orgPhoneUuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgPhoneUuid,
                    Type = AddressRelationType.PHONE
                });
            }

            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("EMAIL"))
            {
                ServiceHelper.UpdateAddress(registration.Email, orgEmailUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.EMAIL
                    });
                }
            }
            else if (orgEmailUuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgEmailUuid,
                    Type = AddressRelationType.EMAIL
                });
            }

            // only update if not ignored, otherwise keep reference if exists, else do nothing
            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("LOCATION"))
            {
                ServiceHelper.UpdateAddress(registration.Location, orgLocationUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.LOCATION
                    });
                }
            }
            else if (orgLocationUuid != null)
            {
                // just keep the old reference if one is available
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgLocationUuid,
                    Type = AddressRelationType.LOCATION
                });
            }

            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("DTRID"))
            {

                ServiceHelper.UpdateAddress(registration.DtrId, orgDtrIdUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.DTR_ID
                    });
                }
            }
            else if (orgDtrIdUuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgDtrIdUuid,
                    Type = AddressRelationType.DTR_ID
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

            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("EAN"))
            {

                ServiceHelper.UpdateAddress(registration.Ean, orgEanUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.EAN
                    });
                }
            }
            else if (orgEanUuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgEanUuid,
                    Type = AddressRelationType.EAN
                });
            }


            ServiceHelper.UpdateAddress(registration.LOSId, orgLosIdUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.LOSID
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

            // only update if not ignored, otherwise keep reference if exists, else do nothing
            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("CONTACT"))
            {
                ServiceHelper.UpdateAddress(registration.Contact, orgContactUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.CONTACT_ADDRESS
                    });
                }
            }
            else if (orgContactUuid != null)
            {
                // just keep the old reference if one is available
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgContactUuid,
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

            ServiceHelper.UpdateAddress(registration.PostSecondary, orgPostSecondaryUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.POST,
                    // not prime, used for sorting later when updating indexes
                    Prime = false
                });
            }

            ServiceHelper.UpdateAddress(registration.FOA, orgFOAUuid, registration.Timestamp, out uuid);
            if (uuid != null)
            {
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = uuid,
                    Type = AddressRelationType.FOA
                });
            }

            // only update if not ignored, otherwise keep reference if exists, else do nothing
            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("PNR"))
            {

                ServiceHelper.UpdateAddress(registration.PNR, orgPNRUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.PNR
                    });
                }
            }
            else if (orgPNRUuid != null)
            {
                // just keep the old reference if one is available
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgPNRUuid,
                    Type = AddressRelationType.PNR
                });
            }

            // only update if not ignored, otherwise keep reference if exists, else do nothing
            if (!OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Contains("SOR"))
            {
                ServiceHelper.UpdateAddress(registration.SOR, orgSORUuid, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.SOR
                    });
                }
            }
            else if (orgSORUuid != null)
            {
                // just keep the old reference if one is available
                addressRefs.Add(new AddressRelation()
                {
                    Uuid = orgSORUuid,
                    Type = AddressRelationType.SOR
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

            if (!string.IsNullOrEmpty(registration.LOSId))
            {
                ServiceHelper.ImportAddress(registration.LOSId, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.LOSID
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

            if (!string.IsNullOrEmpty(registration.DtrId))
            {
                ServiceHelper.ImportAddress(registration.DtrId, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.DTR_ID
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

            if (!string.IsNullOrEmpty(registration.PostSecondary))
            {
                ServiceHelper.ImportAddress(registration.PostSecondary, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.POST,
                        // this ensures that it will get a higher Index
                        Prime = false
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.FOA))
            {
                ServiceHelper.ImportAddress(registration.FOA, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.FOA
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.PNR))
            {
                ServiceHelper.ImportAddress(registration.PNR, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.PNR
                    });
                }
            }

            if (!string.IsNullOrEmpty(registration.SOR))
            {
                ServiceHelper.ImportAddress(registration.SOR, registration.Timestamp, out uuid);
                if (uuid != null)
                {
                    addressRefs.Add(new AddressRelation()
                    {
                        Uuid = uuid,
                        Type = AddressRelationType.SOR
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

            var ou = inspectorService.ReadOUObject(uuid, ReadTasks.YES, ReadManager.YES, ReadAddresses.YES, ReadPayoutUnit.YES, ReadContactPlaces.YES, ReadPositions.NO, ReadContactForTasks.YES);
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
                registration.ItSystems = ou.ItSystems;
                registration.ContactForTasks = ou.ContactForTasks;
                registration.ContactPlaces = ou.ContactPlaces;

                var posts = new List<AddressHolder>();

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
                    else if (address is DTO.Read.LOSID)
                    {
                        registration.LOSId = address.Value;
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
                    else if (address is DTO.Read.DtrId)
                    {
                        registration.DtrId = address.Value;
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
                    else if (address is DTO.Read.FOA)
                    {
                        registration.FOA = address.Value;
                    }
                    else if (address is DTO.Read.PNR)
                    {
                        registration.PNR = address.Value;
                    }
                    else if (address is DTO.Read.SOR)
                    {
                        registration.SOR = address.Value;
                    }
                    else if (address is DTO.Read.Post)
                    {
                        posts.Add(address);
                    }
                    else
                    {
                        log.Warn("Trying to Read OrgUnit " + uuid + " with unknown address type " + address.GetType().ToString());
                    }
                }

                // special handling of Post because we support two variants (sort by index, assuming index is used correctly ;))
                posts.Sort((x, y) => x.AddressIndex.CompareTo(y.AddressIndex));
                foreach (var address in posts)
                {
                    if (registration.Post == null)
                    {
                        registration.Post = address.Value;
                    }
                    else if (registration.PostSecondary == null)
                    {
                        registration.PostSecondary = address.Value;
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
            organisationEnhed.ItSystemUuids = registration.ItSystems;

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

        private bool DisableUdbetalingsenheder()
        {
            if (!string.IsNullOrEmpty(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableUdbetalingsenheder))
            {
                if (OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableUdbetalingsenheder.Contains("true") ||
                    OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableUdbetalingsenheder.Contains(OrganisationRegistryProperties.GetCurrentMunicipality()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool DisableHenvendelsessteder()
        {
            if (!string.IsNullOrEmpty(OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableHenvendelsessteder))
            {
                if (OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableHenvendelsessteder.Contains("true") ||
                    OrganisationRegistryProperties.AppSettings.SchedulerSettings.DisableHenvendelsessteder.Contains(OrganisationRegistryProperties.GetCurrentMunicipality()))
                {
                    return true;
                }
            }

            return false;
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

            if (errors.Count > 0)
            {
                throw new InvalidFieldsException("Invalid registration object - the following fields are invalid: " + string.Join(",", errors));
            }

            if (DisableHenvendelsessteder())
            {
                registration.ContactForTasks = new List<string>();
            }

            if (DisableUdbetalingsenheder())
            {
                registration.PayoutUnitUuid = null;
            }

            registration.Uuid = registration.Uuid.ToLower();
        }
    }
}