using System;
using Organisation.BusinessLayer.DTO.Registration;

namespace OS2syncAD
{
    public class EventMapper
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public UserRegistration MapUser(ADEvent anEvent)
        {
            UserRegistration user = new UserRegistration();
            user.Uuid = anEvent.ADAttributes.Uuid;
            user.Timestamp = anEvent.TimeOcurred;

            if (!anEvent.OperationType.Equals(OperationType.Remove))
            {
                user.UserId = getSingleAttribute(anEvent, "sAMAccountName");

                if (anEvent.ADAttributes.Contains(AppConfiguration.UserAttributePersonName))
                {
                    user.Person.Name = getSingleAttribute(anEvent, AppConfiguration.UserAttributePersonName);
                }

                if (anEvent.ADAttributes.Contains(AppConfiguration.UserAttributePositionName))
                {
                    string positionName = getSingleAttribute(anEvent, AppConfiguration.UserAttributePositionName);

                    user.Positions.Add(new Position()
                    {
                        OrgUnitUuid = anEvent.ParentOUUUID,
                        Name = positionName
                    });
                }
                else
                {
                    log.Debug("User " + user.Person.Name + " did not have a '" + AppConfiguration.UserAttributePositionName + "' attribute set in AD, mapping to default position name 'Ansat'");
                    string positionName = "Ansat";

                    user.Positions.Add(new Position()
                    {
                        OrgUnitUuid = anEvent.ParentOUUUID,
                        Name = positionName
                    });
                }

                if (!string.IsNullOrEmpty(AppConfiguration.UserAttributePhone) && anEvent.ADAttributes.Contains(AppConfiguration.UserAttributePhone))
                {
                    user.PhoneNumber = getSingleAttribute(anEvent, AppConfiguration.UserAttributePhone);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.UserAttributeLocation) && anEvent.ADAttributes.Contains(AppConfiguration.UserAttributeLocation))
                {
                    user.Location = ((ADSingleValueAttribute)anEvent.ADAttributes.GetField(AppConfiguration.UserAttributeLocation)).Value;
                }

                if (!string.IsNullOrEmpty(AppConfiguration.UserAttributeMail) && anEvent.ADAttributes.Contains(AppConfiguration.UserAttributeMail))
                {
                    user.Email = ((ADSingleValueAttribute)anEvent.ADAttributes.GetField(AppConfiguration.UserAttributeMail)).Value;
                }

                if (!string.IsNullOrEmpty(AppConfiguration.UserAttributeRacfID) && anEvent.ADAttributes.Contains(AppConfiguration.UserAttributeRacfID))
                {
                    user.RacfID = ((ADSingleValueAttribute)anEvent.ADAttributes.GetField(AppConfiguration.UserAttributeRacfID)).Value;
                }

                if (!string.IsNullOrEmpty(AppConfiguration.UserAttributePersonCpr) && anEvent.ADAttributes.Contains(AppConfiguration.UserAttributePersonCpr))
                {
                    user.Person.Cpr = getSingleAttribute(anEvent, AppConfiguration.UserAttributePersonCpr);
                }
            }

            return user;
        }

        public OrgUnitRegistration MapOU(ADEvent anEvent)
        {
            OrgUnitRegistration orgUnit = new OrgUnitRegistration();
            orgUnit.Uuid = anEvent.ADAttributes.Uuid;
            orgUnit.Timestamp = anEvent.TimeOcurred;
            orgUnit.ParentOrgUnitUuid = anEvent.ParentOUUUID;

            if (!anEvent.OperationType.Equals(OperationType.Remove))
            {
                // it is possible to configure an alternative field to lookup names
                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeName) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeName))
                {
                    orgUnit.Name = getSingleAttribute(anEvent, AppConfiguration.OUAttributeName);
                    orgUnit.ShortKey = getSingleAttribute(anEvent, AppConfiguration.OUAttributeName);
                }
                else if (anEvent.ADAttributes.Contains("ou")) // default is to use "ou" attribute
                {
                    orgUnit.Name = getSingleAttribute(anEvent, "ou");
                    orgUnit.ShortKey = getSingleAttribute(anEvent, "ou");
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeEan) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeEan))
                {
                    orgUnit.Ean = getSingleAttribute(anEvent, AppConfiguration.OUAttributeEan);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeDtrId) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeDtrId))
                {
                    orgUnit.DtrId = getSingleAttribute(anEvent, AppConfiguration.OUAttributeDtrId);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeEmail) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeEmail))
                {
                    orgUnit.Email = getSingleAttribute(anEvent, AppConfiguration.OUAttributeEmail);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeLocation) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeLocation))
                {
                    orgUnit.Location = getSingleAttribute(anEvent, AppConfiguration.OUAttributeLocation);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeLOSShortName) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeLOSShortName))
                {
                    orgUnit.LOSShortName = getSingleAttribute(anEvent, AppConfiguration.OUAttributeLOSShortName);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributeLOSId) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributeLOSId))
                {
                    orgUnit.LOSId = getSingleAttribute(anEvent, AppConfiguration.OUAttributeLOSId);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributePayoutUnitUUID) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributePayoutUnitUUID))
                {
                    orgUnit.PayoutUnitUuid = getSingleAttribute(anEvent, AppConfiguration.OUAttributePayoutUnitUUID);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributePhone) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributePhone))
                {
                    orgUnit.PhoneNumber = getSingleAttribute(anEvent, AppConfiguration.OUAttributePhone);
                }

                if (!string.IsNullOrEmpty(AppConfiguration.OUAttributePost) && anEvent.ADAttributes.Contains(AppConfiguration.OUAttributePost))
                {
                    orgUnit.Post = getSingleAttribute(anEvent, AppConfiguration.OUAttributePost);
                }
            }

            return orgUnit;
        }

        public static global::Organisation.SchedulingLayer.OperationType Map(OperationType operationType)
        {
            if (OperationType.Remove.Equals(operationType))
            {
                return global::Organisation.SchedulingLayer.OperationType.DELETE;
            }
            else
            {
                return global::Organisation.SchedulingLayer.OperationType.UPDATE;
            }
        }

        private static string getSingleAttribute(ADEvent anEvent, String name)
        {
            IADAttribute attribute = anEvent.ADAttributes.GetField(name);

            if (attribute is ADSingleValueAttribute)
            {
                return ((ADSingleValueAttribute)attribute).Value;
            }
            else if (attribute is ADMultiValueAttribute)
            {
                var values = ((ADMultiValueAttribute)attribute).Values;

                if (values.Count > 0)
                {
                    // someone is using a REG_MULTI_SZ field as a key for a single value element - so we just pick the first record
                    return values[0];
                }
            }

            return null;
        }
    }
}
