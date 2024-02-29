using System;
using System.ServiceModel;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.ServiceModel.Channels;
using System.IdentityModel.Tokens;
using Digst.OioIdws.WscCore.OioWsTrust;
using Digst.OioIdws.OioWsTrustCore;
using Digst.OioIdws.SoapCore;
using Digst.OioIdws.SoapCore.Tokens;
using Digst.OioIdws.SoapCore.Bindings;
using Digst.OioIdws.CommonCore;

namespace Organisation.IntegrationLayer
{
    internal static class StubUtil
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static string GetMunicipalityOrganisationUUID()
        {
            return OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()];
        }

        public static string ConstructSoapErrorMessage(int statusCode, string operation, string service, string FejlbeskedTekst)
        {
            return "Service '" + service + "." + operation + "' returned (" + statusCode + ") with message: " + FejlbeskedTekst;
        }

        public static bool TerminateVirkning(dynamic virkning, DateTime timestamp, bool force = false)
        {
            // make sure TilTidspunkt is always at least FraTidspunkt
            object from = virkning.FraTidspunkt.Item;
            if (from is DateTime)
            {
                DateTime fromDT = (DateTime)from;
                if (DateTime.Compare(fromDT, timestamp) > 0)
                {
                    timestamp = fromDT;
                }
            }

            DateTime endTime = timestamp.Date + new TimeSpan(0, 0, 0);

            object current = virkning.TilTidspunkt.Item;
            if (force || current == null || !(current is DateTime) || (current is DateTime && DateTime.Compare((DateTime) current, endTime) > 0))
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

        public static PortType CreateChannel<PortType>(string service, string operation)
        {
            OioIdwsWcfConfigurationSection wscConfiguration = new OioIdwsWcfConfigurationSection
            {

                StsEndpointAddress = OrganisationRegistryProperties.AppSettings.StsSettings.StsEndpointAddress,
                StsEntityIdentifier = OrganisationRegistryProperties.AppSettings.StsSettings.StsEntityIdentifier,

                StsCertificate = new Certificate
                {
                    FromFileSystem = true,
                    FilePath = OrganisationRegistryProperties.AppSettings.StsSettings.StsCertificateLocation
                },

                WspEndpoint = OrganisationRegistryProperties.AppSettings.ServiceSettings.WspEndpointBaseUrl + service + "/",
                WspEndpointID = OrganisationRegistryProperties.AppSettings.ServiceSettings.WspEndpointID,
                WspSoapVersion = "1.2",

                ServiceCertificate = new Certificate
                {
                    FromFileSystem = true,
                    FilePath = OrganisationRegistryProperties.AppSettings.ServiceSettings.WspCertificateLocation
                },

                ClientCertificate = new Certificate
                {
                    FromFileSystem = true,
                    FilePath = OrganisationRegistryProperties.AppSettings.ClientSettings.WscKeystoreLocation,
                    Password = OrganisationRegistryProperties.AppSettings.ClientSettings.WscKeystorePassword,
                },

                Cvr = OrganisationRegistryProperties.GetCurrentMunicipality(),
                TokenLifeTimeInMinutes = 120,
                IncludeLibertyHeader = false,
                MaxReceivedMessageSize = Int32.MaxValue
            };

            StsTokenServiceConfiguration stsConfiguration = TokenServiceConfigurationFactory.CreateConfiguration(wscConfiguration);

            if (OrganisationRegistryProperties.AppSettings.TrustAllCertificates)
            {
                stsConfiguration.SslCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;
                stsConfiguration.StsCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;
                stsConfiguration.WspCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;

                stsConfiguration.SslCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                stsConfiguration.StsCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                stsConfiguration.WspCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
            }

            IStsTokenService stsTokenService = new StsTokenServiceCache(stsConfiguration);
            var securityToken = (GenericXmlSecurityToken)stsTokenService.GetToken();

            if (!securityToken.TokenXml.OuterXml.Contains(OrganisationRegistryProperties.GetCurrentMunicipality()))
            {
                throw new Exception("Token from STS does not contain CVR of municipality: " + OrganisationRegistryProperties.GetCurrentMunicipality());
            }

            return CreateChannelWithIssuedToken<PortType>(securityToken, stsConfiguration, service, operation);
        }

        /// <summary>
        /// An equivalent version of the familiar CreateChannelWithIssuedToken helper method in the .NET Framework
        /// </summary>
        /// <typeparam name="T">Type of the service</typeparam>
        /// <param name="token">A security token which is issued by an STS</param>
        /// <param name="stsConfiguration">Configuration of the STS and the WSP</param>
        /// <returns>A channel to call service T</returns>
        public static T CreateChannelWithIssuedToken<T>(GenericXmlSecurityToken token, StsTokenServiceConfiguration stsConfiguration, string service, string operation)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));
            if (stsConfiguration == null)
                throw new ArgumentNullException(nameof(stsConfiguration));

            // IMPORTANT: https://devblogs.microsoft.com/dotnet/wsfederationhttpbinding-in-net-standard-wcf/
            // First, create the inner binding for communicating with the token issuer.
            // The security settings will be specific to the STS and should mirror what
            // would have been in an app.config in a .NET Framework scenario.

            var serverCertificate = stsConfiguration.WspConfiguration.ServiceCertificate;
            var messageVersion = MessageVersion.CreateVersion(stsConfiguration.WspConfiguration.SoapVersion, AddressingVersion.WSAddressing10);

            // Create a token parameters. The token is then used by FederatedChannelSecurityTokenManager to create an instance of FederatedTokenSecurityTokenProvider which returns the token immediately
            var tokenParameters = new FederatedSecurityTokenParameters(token, messageVersion, stsConfiguration, stsConfiguration.WspConfiguration.EndpointAddress)
            {
                MessageSecurityVersion = MessageSecurityVersion.WSSecurity11WSTrust13WSSecureConversation13WSSecurityPolicy12BasicSecurityProfile10,
                MaxReceivedMessageSize = stsConfiguration.MaxReceivedMessageSize,
                IncludeLibertyHeader = stsConfiguration.IncludeLibertyHeader,
            };

            var bindingToCallService = new OioIdwsSoapBinding(tokenParameters);
            FederatedChannelFactory<T> factory = CreateFactory<T>(stsConfiguration, serverCertificate, bindingToCallService);

            if (OrganisationRegistryProperties.AppSettings.LogSettings.LogRequestResponse)
            {
                factory.Endpoint.EndpointBehaviors.Add(new LoggingBehavior(service, operation));
            }

            factory.Endpoint.EndpointBehaviors.Add(new RequestHeaderBehavior());

            // .NET Core does not support asymmetric binding, so it does not call the CreateSecurityTokenAuthenticator method to create an X509SecurityTokenAuthenticator to validate the service certificate
            // Implement a custom X509SecurityTokenAuthenticator is not an option because not all necessary types used by that abstract class is exposed to .NET Core
            stsConfiguration.WspCertificateAuthentication.Validate(serverCertificate);

            return factory.CreateChannel();
        }

        private static FederatedChannelFactory<T> CreateFactory<T>(IStsTokenServiceConfiguration stsConfiguration, X509Certificate2 serverCertificate, OioIdwsSoapBinding bindingToCallService)
        {
            // we need to create a client 
            var factory = new FederatedChannelFactory<T>(bindingToCallService, new EndpointAddress(stsConfiguration.WspConfiguration.EndpointAddress));
            factory.Credentials.ServiceCertificate.Authentication.CopyFrom(stsConfiguration.WspCertificateAuthentication);
            factory.Credentials.ServiceCertificate.SslCertificateAuthentication = stsConfiguration.SslCertificateAuthentication.DeepClone();

            string dnsName = serverCertificate.GetNameInfo(X509NameType.DnsName, false);
            EndpointIdentity identity = new DnsEndpointIdentity(dnsName);
            EndpointAddress endpointAddress = new EndpointAddress(new Uri(stsConfiguration.WspConfiguration.EndpointAddress), identity);
            factory.Endpoint.Address = endpointAddress;
            factory.Credentials.ClientCertificate.Certificate = stsConfiguration.ClientCertificate;
            factory.Credentials.ServiceCertificate.ScopedCertificates.Add(endpointAddress.Uri, serverCertificate);
            return factory;
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

        public static bool TerminateObjectsInOrgFromUuidList(dynamic orgArray, List<string> toTerminate, DateTime timestamp)
        {
            bool changes = false;

            if (orgArray != null && toTerminate != null)
            {
                foreach (var objectInOrg in orgArray)
                {
                    foreach (var term in toTerminate)
                    {
                        if (term.Equals(objectInOrg.ReferenceID.Item))
                        {
                            if (StubUtil.TerminateVirkning(objectInOrg.Virkning, timestamp))
                            {
                                changes = true;
                            }

                            break;
                        }
                    }
                }
            }

            return changes;
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
