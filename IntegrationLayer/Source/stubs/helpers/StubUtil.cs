using System;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using IntegrationLayer.OrganisationFunktion;

namespace Organisation.IntegrationLayer
{
    internal static class StubUtil
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static OrganisationRegistryProperties registryProperties = OrganisationRegistryProperties.GetInstance();

        public static string GetMunicipalityOrganisationUUID()
        {
            return registryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()];
        }

        public static EndpointAddress GetEndPointAddress(string suffix)
        {
            EndpointAddress endPointAddress = new EndpointAddress(new Uri(registryProperties.ServicesBaseUrl + suffix));

            return endPointAddress;
        }

        public static string ConstructSoapErrorMessage(int statusCode, string operation, string service, string FejlbeskedTekst)
        {
            return "Service '" + service + "." + operation + "' returned (" + statusCode + ") with message: " + FejlbeskedTekst;
        }

        public static Uri GetUri(string suffix)
        {
            return new Uri(registryProperties.ServicesBaseUrl + suffix);
        }

        public static bool TerminateVirkning(dynamic virkning, DateTime timestamp)
        {
            DateTime endTime = timestamp.Date + new TimeSpan(0, 0, 0);

            object current = virkning.TilTidspunkt.Item;
            if (current == null || !(current is DateTime) || (current is DateTime && DateTime.Compare((DateTime) current, endTime) > 0))
            {
                virkning.TilTidspunkt.Item = endTime;
                return true;
            }

            return false;
        }

        public static T GetReference<T>(string uuid, dynamic type) where T : new()
        {
            dynamic reference = new T();
            reference.Item = uuid;
            reference.ItemElementName = type;

            return reference;
        }

        public static AdressePortType CreateChannel<AdressePortType>(string service, string operation, dynamic port)
        {
            Uri uri = GetUri(service);

            if (registryProperties.LogRequestResponse)
            {
                port.ChannelFactory.Endpoint.EndpointBehaviors.Add(new LoggingBehavior(service, operation));
            }

            port.ChannelFactory.Endpoint.EndpointBehaviors.Add(new RequestHeaderBehavior());

            return port.ChannelFactory.CreateChannel();
        }

        public static EgenskabType GetLatestProperty<EgenskabType>(EgenskabType[] properties)
        {
            if (properties == null)
            {
                return default(EgenskabType);
            }

            foreach (dynamic property in properties)
            {
                object endTime = property.Virkning.TilTidspunkt.Item;

                // either the registration is open-ended, or the set TilTidspunkt is after NOW
                if (!(endTime is DateTime) || (DateTime.Compare(DateTime.Now, (DateTime)endTime) < 0))
                {
                    return property;
                }
            }

            return default(EgenskabType);
        }

        public static GyldighedType GetLatestGyldighed<GyldighedType>(GyldighedType[] states)
        {
            if (states == null)
            {
                return default(GyldighedType);
            }

            foreach (dynamic state in states)
            {
                object endTime = state.Virkning.TilTidspunkt.Item;
                
                // either the registration is open-ended, or the set TilTidspunkt is after NOW
                if (!(endTime is DateTime) || (DateTime.Compare(DateTime.Now, (DateTime)endTime) < 0))
                {
                    return state;
                }
            }

            return default(GyldighedType);
        }

        public static bool TerminateObjectsInOrgNoLongerPresentLocally(dynamic orgArray, dynamic localArray, DateTime timestamp, bool uuidSubReference)
        {
            bool changes = false;

            if (orgArray != null)
            {
                foreach (var objectInOrg in orgArray)
                {
                    bool found = false;

                    if (localArray != null)
                    {
                        foreach (var objectInLocal in localArray)
                        {
                            // we need the UuidSubReference check on both sides of the || to ensure that the second part of the && clause is not evaluated unless needed
                            if ((uuidSubReference && objectInLocal.Uuid.Equals(objectInOrg.ReferenceID.Item)) || (!uuidSubReference && objectInLocal.Equals(objectInOrg.ReferenceID.Item)))
                            {
                                found = true;
                            }
                        }
                    }

                    if (!found)
                    {
                        // as this is a FlerRelation, we also get all the old references, so only mark the object as
                        // changed if we actually terminate a valid Virkning
                        if (StubUtil.TerminateVirkning(objectInOrg.Virkning, timestamp))
                        {
                            changes = true;
                        }
                    }
                }
            }

            return changes;
        }

        public static List<string> FindAllObjectsInLocalNotInOrg(dynamic orgArray, dynamic localArray, bool uuidSubReference)
        {
            List<string> uuidsToAdd = new List<string>();

            if (localArray != null)
            {
                foreach (var objectInLocal in localArray)
                {
                    bool found = false;

                    if (orgArray != null)
                    {
                        foreach (var objectInOrg in orgArray)
                        {
                            if ((uuidSubReference && objectInLocal.Uuid.Equals(objectInOrg.ReferenceID.Item)) || (!uuidSubReference && objectInLocal.Equals(objectInOrg.ReferenceID.Item)))
                            {
                                var endTime = objectInOrg.Virkning.TilTidspunkt.Item;

                                // endTime is bool => ok
                                // endTime is DateTime, but Now is before endTime => ok
                                if (!(endTime is DateTime) || (DateTime.Compare(DateTime.Now, (DateTime)endTime) < 0))
                                {
                                    found = true;
                                }
                            }
                        }
                    }

                    if (!found)
                    {
                        uuidsToAdd.Add((uuidSubReference) ? objectInLocal.Uuid : objectInLocal);
                    }
                }
            }

            return uuidsToAdd;
        }
    }
}
