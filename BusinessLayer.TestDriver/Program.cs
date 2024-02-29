using System;
using Organisation.BusinessLayer.DTO.Registration;
using Organisation.IntegrationLayer;
using System.Collections.Generic;
using System.IO;
using Digst.OioIdws.OioWsTrustCore;
using Digst.OioIdws.WscCore.OioWsTrust;
using System.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace Organisation.BusinessLayer.TestDriver
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static OrgUnitService orgUnitService = new OrgUnitService();
        private static UserService userService = new UserService();
        private static InspectorService inspectorService = new InspectorService();

        private static void InitEnvironment()
        {
            System.Environment.SetEnvironmentVariable("ClientSettings__WscKeystoreLocation", "c:/certifikater/keystore.pfx");
            System.Environment.SetEnvironmentVariable("ClientSettings__WscKeystorePassword", "xxxx");
            System.Environment.SetEnvironmentVariable("StsSettings__StsCertificateLocation", Path.Combine(Directory.GetCurrentDirectory(), "../../../../Resources/cert/test-sts.cer"));
            System.Environment.SetEnvironmentVariable("ServiceSettings__WspCertificateLocation", Path.Combine(Directory.GetCurrentDirectory(), "../../../../Resources/cert/test-sf1500.cer"));
            System.Environment.SetEnvironmentVariable("Environment", "TEST");
            System.Environment.SetEnvironmentVariable("LogSettings__LogRequestResponse", "true");
            System.Environment.SetEnvironmentVariable("Cvr", "12345678");
            System.Environment.SetEnvironmentVariable("TrustAllCertificates", "true");

            Initializer.Init();

            // hack to ensure random org-uuid to avoid data conflicts
            OrganisationRegistryProperties.MunicipalityOrganisationUUID.Remove(System.Environment.GetEnvironmentVariable("Cvr"));
            OrganisationRegistryProperties.MunicipalityOrganisationUUID.Add(System.Environment.GetEnvironmentVariable("Cvr"), Guid.NewGuid().ToString().ToLower());
        }        

        static void Main(string[] args)
        {
            InitEnvironment();

            /* ordinary tests
            TestTokens();
            TestIgnoreAddress();
            TestCreateAndUpdateFullUser();
            TestItSystems();
            TestListAndReadOUs();
            TestListAndReadUsers();
            TestSecondaryPost();
            TestCreateDeleteUpdateUser();
            TestCreateDeleteUpdateOU();
            TestCreateAndUpdateFullOU();
            TestUpdateWithoutChanges();
            TestPayoutUnits();
            TestPositions();
            TestUpdateAndSearch();
            TestMultipleAddresses();
            */
            System.Environment.Exit(0);
        }

        private static void TestIgnoreAddress()
        {
            OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Clear();
            OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Add("SOR");
            OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Add("EMAIL");

            var reg = OUReg();
            reg.SOR = "SOR-1234";
            reg.Email = "Email-1234";
            reg.Landline = "Landline-1234";
            orgUnitService.Update(reg);

            var ou = orgUnitService.Read(reg.Uuid);

            reg.SOR = "SOR-4321";
            reg.Email = "Email-4321";
            reg.Landline = "Landline-4321";
            orgUnitService.Update(reg);

            var ou2 = orgUnitService.Read(reg.Uuid);

            if (ou2.SOR.Equals(reg.SOR))
            {
                throw new Exception("SOR was updatd, even though it was ignored");
            }

            if (ou2.Email.Equals(reg.Email))
            {
                throw new Exception("Email was updated, even though it was ignored");
            }

            if (!ou2.Landline.Equals(reg.Landline))
            {
                throw new Exception("Landline was NOT updated, but it was not ignored, so what gives?");
            }

            // reset for other tests
            OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Clear();
            OrganisationRegistryProperties.AppSettings.SchedulerSettings.IgnoredOUAddressTypes.Add("SOR");
        }

        private static void TestTokens()
        {
            OioIdwsWcfConfigurationSection wscConfiguration1 = new OioIdwsWcfConfigurationSection
            {

                StsEndpointAddress = OrganisationRegistryProperties.AppSettings.StsSettings.StsEndpointAddress,
                StsEntityIdentifier = OrganisationRegistryProperties.AppSettings.StsSettings.StsEntityIdentifier,

                StsCertificate = new Certificate
                {
                    FromFileSystem = true,
                    FilePath = OrganisationRegistryProperties.AppSettings.StsSettings.StsCertificateLocation
                },

                WspEndpoint = OrganisationRegistryProperties.AppSettings.ServiceSettings.WspEndpointBaseUrl + "adresse/6/",
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

                Cvr = "29189633",
                TokenLifeTimeInMinutes = 120,
                IncludeLibertyHeader = false,
                MaxReceivedMessageSize = Int32.MaxValue
            };

            OioIdwsWcfConfigurationSection wscConfiguration2 = new OioIdwsWcfConfigurationSection
            {

                StsEndpointAddress = OrganisationRegistryProperties.AppSettings.StsSettings.StsEndpointAddress,
                StsEntityIdentifier = OrganisationRegistryProperties.AppSettings.StsSettings.StsEntityIdentifier,

                StsCertificate = new Certificate
                {
                    FromFileSystem = true,
                    FilePath = OrganisationRegistryProperties.AppSettings.StsSettings.StsCertificateLocation
                },

                WspEndpoint = OrganisationRegistryProperties.AppSettings.ServiceSettings.WspEndpointBaseUrl + "adresse/6/",
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

                Cvr = "29189978",
                TokenLifeTimeInMinutes = 120,
                IncludeLibertyHeader = false,
                MaxReceivedMessageSize = Int32.MaxValue
            };

            StsTokenServiceConfiguration stsConfiguration1 = TokenServiceConfigurationFactory.CreateConfiguration(wscConfiguration1);
            StsTokenServiceConfiguration stsConfiguration2 = TokenServiceConfigurationFactory.CreateConfiguration(wscConfiguration2);

            if (OrganisationRegistryProperties.AppSettings.TrustAllCertificates)
            {
                stsConfiguration1.SslCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;
                stsConfiguration1.StsCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;
                stsConfiguration1.WspCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;

                stsConfiguration1.SslCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                stsConfiguration1.StsCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                stsConfiguration1.WspCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;

                stsConfiguration2.SslCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;
                stsConfiguration2.StsCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;
                stsConfiguration2.WspCertificateAuthentication.RevocationMode = X509RevocationMode.NoCheck;

                stsConfiguration2.SslCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                stsConfiguration2.StsCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                stsConfiguration2.WspCertificateAuthentication.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;

            }

            IStsTokenService stsTokenService1 = new StsTokenServiceCache(stsConfiguration1);
            var securityToken1 = (GenericXmlSecurityToken)stsTokenService1.GetToken();

            if (!securityToken1.TokenXml.OuterXml.Contains("29189633"))
            {
                throw new Exception("Token from STS does not contain CVR of municipality: 29189633");
            }

            IStsTokenService stsTokenService2 = new StsTokenServiceCache(stsConfiguration2);
            var securityToken2 = (GenericXmlSecurityToken)stsTokenService2.GetToken();

            if (!securityToken2.TokenXml.OuterXml.Contains("29189978"))
            {
                throw new Exception("Token from STS does not contain CVR of municipality: 29189978");
            }
        }

        private static void TestItSystems()
        {
            var reg = OUReg();
            orgUnitService.Update(reg);

            var ou = orgUnitService.Read(reg.Uuid);
            if (ou.ItSystems.Count != 0)
            {
                throw new Exception("Unexpected amount of it-systems");
            }

            string uuid1 = Guid.NewGuid().ToString().ToLower();
            string uuid2 = Guid.NewGuid().ToString().ToLower();
            string uuid3 = Guid.NewGuid().ToString().ToLower();
            string uuid4 = Guid.NewGuid().ToString().ToLower();

            reg.ItSystems = new List<string>();
            reg.ItSystems.Add(uuid1);
            reg.ItSystems.Add(uuid2);
            orgUnitService.Update(reg);

            ou = orgUnitService.Read(reg.Uuid);
            if (ou.ItSystems.Count != 2)
            {
                throw new Exception("Unexpected amount of it-systems");
            }

            reg.ItSystems = new List<string>();
            reg.ItSystems.Add(uuid2);
            reg.ItSystems.Add(uuid3);
            reg.ItSystems.Add(uuid4);
            orgUnitService.Update(reg);

            ou = orgUnitService.Read(reg.Uuid);
            if (ou.ItSystems.Count != 3)
            {
                throw new Exception("Unexpected amount of it-systems");
            }
        }

        private static void TestMultipleAddresses()
        {
            var reg = OUReg();
            reg.Email = "email@email.com";
            reg.PhoneNumber = "12345678";
            orgUnitService.Update(reg);

            var ou = orgUnitService.Read(reg.Uuid);
            if (!"12345678".Equals(ou.PhoneNumber))
            {
                throw new Exception("Wrong phone");
            }
            else if (!"email@email.com".Equals(ou.Email))
            {
                throw new Exception("Wrong email");
            }
        }

        private static void TestUpdateAndSearch()
        {
            var ouReg1 = OUReg();
            ouReg1.Name = "ou1";
            orgUnitService.Update(ouReg1);

            var ouReg2 = OUReg();
            ouReg2.Name = "ou2";
            orgUnitService.Update(ouReg2);

            var userReg = UserReg();
            userReg.Person.Name = "name";            
            userReg.Positions.Add(new Position()
            {
                Name = "position 1",
                OrgUnitUuid = ouReg1.Uuid                
            });
            userReg.Positions.Add(new Position()
            {
                Name = "position 2",
                OrgUnitUuid = ouReg2.Uuid
            });
            userService.Update(userReg);

            userReg.Positions.Remove(userReg.Positions[0]);
            userService.Update(userReg);

            var user = userService.Read(userReg.Uuid);
            if (user.Positions.Count != 1)
            {
                throw new Exception("User position count should be 1 not " + user.Positions.Count);
            }
        }

        private static void TestListAndReadUsers()
        {
            // small hack to ensure this test passes (the search parameters will find all users in the organisation, and we need to test that it hits the required amount)
            string oldUuid = OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()];
            OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()] = Uuid();

            UserRegistration registration1 = UserReg();
            registration1.UserId = "userId1";
            registration1.Email = "email1@email.com";
            registration1.Person.Name = "Name of Person 1";
            registration1.Positions.Add(new Position()
            {
                Name = "Position 1",
                OrgUnitUuid = Uuid()
            });
            registration1.Positions.Add(new Position()
            {
                Name = "Position 2",
                OrgUnitUuid = Uuid()
            });
            userService.Update(registration1);

            UserRegistration registration2 = UserReg();
            registration2.UserId = "userId2";
            registration2.Email = "email2@email.com";
            registration2.Person.Name = "Name of Person 2";
            registration2.Positions.Add(new Position()
            {
                Name = "Position 3",
                OrgUnitUuid = Uuid()
            });
            registration2.Positions.Add(new Position()
            {
                Name = "Position 4",
                OrgUnitUuid = Uuid()
            });
            userService.Update(registration2);

            UserRegistration registration3 = UserReg();
            registration3.UserId = "userId3";
            registration3.Email = "email3@email.com";
            registration3.Person.Name = "Name of Person 3";
            registration3.Positions.Add(new Position()
            {
                Name = "Position 5",
                OrgUnitUuid = Uuid()
            });
            userService.Update(registration3);
            userService.Delete(registration3.Uuid, DateTime.Now);

            List<string> users = userService.List();
            if (users.Count != 2)
            {
                throw new Exception("List() returned " + users.Count + " users, but 2 was expected");
            }

            foreach (var uuid in users)
            {
                UserRegistration registration = userService.Read(uuid);

                if (uuid.Equals(registration1.Uuid))
                {
                    if (!registration1.UserId.Equals(registration.UserId))
                    {
                        throw new Exception("userId does not match");
                    }

                    if (!registration1.Person.Name.Equals(registration.Person.Name))
                    {
                        throw new Exception("Name does not match");
                    }

                    if (!registration1.Email.Equals(registration.Email))
                    {
                        throw new Exception("Email does not match");
                    }

                    if (registration1.Positions.Count != registration.Positions.Count)
                    {
                        throw new Exception("Amount of positions does not match");
                    }

                    foreach (var position in registration1.Positions)
                    {
                        bool found = false;

                        foreach (var readPosition in registration.Positions)
                        {
                            if (readPosition.Name.Equals(position.Name) && readPosition.OrgUnitUuid.Equals(position.OrgUnitUuid))
                            {
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            throw new Exception("Missing position");
                        }
                    }
                }
                else if (uuid.Equals(registration2.Uuid))
                {
                    if (!registration2.UserId.Equals(registration.UserId))
                    {
                        throw new Exception("userId does not match");
                    }

                    if (!registration2.Person.Name.Equals(registration.Person.Name))
                    {
                        throw new Exception("Name does not match");
                    }

                    if (!registration2.Email.Equals(registration.Email))
                    {
                        throw new Exception("Email does not match");
                    }

                    if (registration2.Positions.Count != registration.Positions.Count)
                    {
                        throw new Exception("Amount of positions does not match");
                    }

                    foreach (var position in registration2.Positions)
                    {
                        bool found = false;

                        foreach (var readPosition in registration.Positions)
                        {
                            if (readPosition.Name.Equals(position.Name) && readPosition.OrgUnitUuid.Equals(position.OrgUnitUuid))
                            {
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            throw new Exception("Missing position");
                        }
                    }
                }
                else
                {
                    throw new Exception("List returned the uuid of an unexpected user");
                }
            }

            OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()] = oldUuid;
        }

        private static void TestListAndReadOUs()
        {
            // small hack to ensure this test passes (the search parameters will find all ous in the organisation, and we need to test that it hits the required amount)
            string oldUuid = OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()];
            OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()] = Uuid();

            OrgUnitRegistration registration1 = OUReg();
            registration1.Name = "magic";
            registration1.Email = "email1@email.com";
            registration1.ParentOrgUnitUuid = Uuid();
            orgUnitService.Update(registration1);

            orgUnitService.Read(registration1.Uuid);

            OrgUnitRegistration registration2 = OUReg();
            registration2.Name = "magic";
            registration2.Email = "email2@email.com";
            registration2.ParentOrgUnitUuid = Uuid();
            orgUnitService.Update(registration2);

            registration2.Name = "different name";
            orgUnitService.Update(registration2);

            // TODO: a KMD bug prevents this test from working...
            OrgUnitRegistration registration3 = OUReg();
            registration3.Name = "ou3";
            registration3.Email = "email3@email.com";
            registration3.ParentOrgUnitUuid = Uuid();
            orgUnitService.Update(registration3);
            orgUnitService.Delete(registration3.Uuid, DateTime.Now);

            List<string> ous = orgUnitService.List();
            if (ous.Count != 2)
            {
                throw new Exception("List() returned " + ous.Count + " ous, but 2 was expected");
            }

            foreach (var uuid in ous)
            {
                OrgUnitRegistration registration = orgUnitService.Read(uuid);

                if (uuid.Equals(registration1.Uuid))
                {
                    if (!registration1.Name.Equals(registration.Name))
                    {
                        throw new Exception("Name does not match");
                    }

                    if (!registration1.ParentOrgUnitUuid.Equals(registration.ParentOrgUnitUuid))
                    {
                        throw new Exception("ParentOU UUID does not match");
                    }

                    if (!registration1.Email.Equals(registration.Email))
                    {
                        throw new Exception("Email does not match");
                    }
                }
                else if (uuid.Equals(registration2.Uuid))
                {
                    if (!registration2.Name.Equals(registration.Name))
                    {
                        throw new Exception("Name does not match");
                    }

                    if (!registration2.ParentOrgUnitUuid.Equals(registration.ParentOrgUnitUuid))
                    {
                        throw new Exception("ParentOU UUID does not match");
                    }

                    if (!registration2.Email.Equals(registration.Email))
                    {
                        throw new Exception("Email does not match");
                    }
                }
                else
                {
                    throw new Exception("List returned the uuid of an unexpected ou");
                }
            }

            OrganisationRegistryProperties.MunicipalityOrganisationUUID[OrganisationRegistryProperties.GetCurrentMunicipality()] = oldUuid;
        }

        private static void TestPositions()
        {
            string orgUnitUuid1 = Uuid();
            string orgUnitUuid2 = Uuid();
            string orgUnitUuid3 = Uuid();

            // simple employement
            UserRegistration registration = UserReg();
            registration.Positions.Add(new Position()
            {
                Name = "Position 1",
                OrgUnitUuid = orgUnitUuid1
            });

            userService.Update(registration);
            var user = inspectorService.ReadUserObject(registration.Uuid);
            ValidateUser(user, registration);

            // fire from current position, but give two new positions instead
            registration.Positions.Clear();
            registration.Positions.Add(new Position()
            {
                Name = "Position 2",
                OrgUnitUuid = orgUnitUuid2
            });
            registration.Positions.Add(new Position()
            {
                Name = "Position 3",
                OrgUnitUuid = orgUnitUuid3
            });

            userService.Update(registration);
            user = inspectorService.ReadUserObject(registration.Uuid);
            ValidateUser(user, registration);

            // now fire one of those positions
            registration.Positions.Clear();
            registration.Positions.Add(new Position()
            {
                Name = "Position 2",
                OrgUnitUuid = orgUnitUuid2
            });
            userService.Update(registration);
            user = inspectorService.ReadUserObject(registration.Uuid);
            ValidateUser(user, registration);
        }

        private static void TestPayoutUnits()
        {
            OrgUnitRegistration payoutUnit1 = OUReg();
            payoutUnit1.Name = "Udbetalingsenhed 1";
            payoutUnit1.LOSShortName = "UDE1";
            orgUnitService.Update(payoutUnit1);

            OrgUnitRegistration payoutUnit2 = OUReg();
            payoutUnit2.Name = "Udbetalingsenhed 2";
            payoutUnit2.LOSShortName = "UDE2";
            orgUnitService.Update(payoutUnit2);

            OrgUnitRegistration unit = OUReg();
            unit.Name = "Aktiv enhed";
            orgUnitService.Update(unit);

            var ou = inspectorService.ReadOUObject(unit.Uuid);
            ValidateOU(ou, unit);

            unit.PayoutUnitUuid = payoutUnit1.Uuid;
            orgUnitService.Update(unit);

            ou = inspectorService.ReadOUObject(unit.Uuid);
            ValidateOU(ou, unit);

            // lock another active unit onto this payout unit
            OrgUnitRegistration unit2 = OUReg();
            unit2.Name = "Aktiv enhed 2";
            unit2.PayoutUnitUuid = payoutUnit1.Uuid;
            orgUnitService.Update(unit2);

            unit.PayoutUnitUuid = payoutUnit2.Uuid;
            orgUnitService.Update(unit);

            ou = inspectorService.ReadOUObject(unit.Uuid);
            ValidateOU(ou, unit);

            // test that the other active units kept its old reference
            ou = inspectorService.ReadOUObject(unit2.Uuid);
            ValidateOU(ou, unit2);

            unit.PayoutUnitUuid = null;
            orgUnitService.Update(unit);

            ou = inspectorService.ReadOUObject(unit.Uuid);
            ValidateOU(ou, unit);

            unit.PayoutUnitUuid = payoutUnit1.Uuid;
            orgUnitService.Update(unit);

            ou = inspectorService.ReadOUObject(unit.Uuid);
            ValidateOU(ou, unit);

            orgUnitService.Delete(payoutUnit1.Uuid, DateTime.Now.AddMinutes(-2));

            // special test-case - when deling the payout unit, the active unit should still point to the payout unit (deleted or otherwise). It is the callers responsibility to update the active units as well
            ou = inspectorService.ReadOUObject(unit.Uuid);
            ValidateOU(ou, unit);
        }

        private static void BuildTestData()
        {
            OrgUnitRegistration root = OUReg();
            root.Name = "Fiskebæk Kommune";
            orgUnitService.Update(root);

            UserRegistration user = UserReg();
            user.Person.Name = "Mads Langkilde";
            user.UserId = "mlk";
            var position = new Position();
            user.Positions.Add(position);
            position.Name = "Mayor";
            position.OrgUnitUuid = root.Uuid;
            userService.Update(user);

            OrgUnitRegistration administration = OUReg();
            administration.Name = "Administration";
            administration.ParentOrgUnitUuid = root.Uuid;
            orgUnitService.Update(administration);

            OrgUnitRegistration economics = OUReg();
            economics.Name = "Økonomi";
            economics.ParentOrgUnitUuid = administration.Uuid;
            orgUnitService.Update(economics);

            OrgUnitRegistration fireDepartment = OUReg();
            fireDepartment.Name = "Brandvæsenet";
            fireDepartment.ParentOrgUnitUuid = administration.Uuid;
            orgUnitService.Update(fireDepartment);

            user = UserReg();
            user.Person.Name = "Bente Blankocheck";
            user.UserId = "bbcheck";
            position = new Position()
            {
                Name = "Leder",
                OrgUnitUuid = economics.Uuid
            };
            user.Positions.Add(position);
            position = new Position()
            {
                Name = "Brandmand",
                OrgUnitUuid = fireDepartment.Uuid
            };
            user.Positions.Add(position);
            userService.Update(user);

            user = UserReg();
            user.Person.Name = "Morten Massepenge";
            user.UserId = "mpenge";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Økonomimedarbejder";
            position.OrgUnitUuid = economics.Uuid;
            userService.Update(user);

            OrgUnitRegistration borgerservice = OUReg();
            borgerservice.Name = "Borgerservice";
            borgerservice.LOSShortName = "BS"; // BorgerService is a payout unit
            borgerservice.ParentOrgUnitUuid = administration.Uuid;
            orgUnitService.Update(borgerservice);

            user = UserReg();
            user.Person.Name = "Karen Hjælpsom";
            user.UserId = "khj";
            user.Email = "khj@mail.dk";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Sagsbehandler";
            position.OrgUnitUuid = borgerservice.Uuid;
            userService.Update(user);

            user = UserReg();
            user.Person.Name = "Søren Sørensen";
            user.UserId = "ssø";
            user.PhoneNumber = "12345678";
            user.Landline = "87654321";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Sagsbehandler";
            position.OrgUnitUuid = borgerservice.Uuid;
            userService.Update(user);

            user = UserReg();
            user.Person.Name = "Viggo Mortensen";
            user.UserId = "vmort";
            user.Person.Cpr = "0101010101";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Sagsbehandler";
            position.OrgUnitUuid = borgerservice.Uuid;
            userService.Update(user);

            OrgUnitRegistration jobcenter = OUReg();
            jobcenter.Name = "Jobcenter Centralkontor";
            jobcenter.PayoutUnitUuid = borgerservice.Uuid;
            jobcenter.Ean = "12312312312312";
            jobcenter.EmailRemarks = "Some remark";
            jobcenter.ParentOrgUnitUuid = administration.Uuid;
            orgUnitService.Update(jobcenter);

            user = UserReg();
            user.Person.Name = "Johan Jensen";
            user.UserId = "jojens";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Leder";
            position.OrgUnitUuid = jobcenter.Uuid;
            userService.Update(user);

            OrgUnitRegistration jobcenter1 = OUReg();
            jobcenter1.Name = "Jobcenter Vest";
            jobcenter1.ParentOrgUnitUuid = jobcenter.Uuid;
            orgUnitService.Update(jobcenter1);

            user = UserReg();
            user.Person.Name = "Julie Jensen";
            user.UserId = "juljen";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Sagsbehandler";
            position.OrgUnitUuid = jobcenter1.Uuid;
            userService.Update(user);

            OrgUnitRegistration jobcenter2 = OUReg();
            jobcenter2.Name = "Jobcenter Øst";
            jobcenter2.ParentOrgUnitUuid = jobcenter.Uuid;
            orgUnitService.Update(jobcenter2);

            OrgUnitRegistration itDepartment = OUReg();
            itDepartment.Name = "IT Department";
            itDepartment.PayoutUnitUuid = borgerservice.Uuid;
            itDepartment.ParentOrgUnitUuid = root.Uuid;
            orgUnitService.Update(itDepartment);

            OrgUnitRegistration operations = OUReg();
            operations.Name = "Drift og operation";
            operations.ParentOrgUnitUuid = itDepartment.Uuid;
            orgUnitService.Update(operations);

            user = UserReg();
            user.Person.Name = "Steven Sørensen";
            user.UserId = "ssøren";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Administrator";
            position.OrgUnitUuid = operations.Uuid;
            userService.Update(user);

            user = UserReg();
            user.Person.Name = "Marie Marolle";
            user.UserId = "marolle";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Administrator";
            position.OrgUnitUuid = operations.Uuid;
            userService.Update(user);

            OrgUnitRegistration projects = OUReg();
            projects.Name = "Udvikling og projekter";
            projects.ParentOrgUnitUuid = itDepartment.Uuid;
            orgUnitService.Update(projects);

            user = UserReg();
            user.Person.Name = "Henrik Jeppesen";
            user.UserId = "jeppe";
            position = new Position();
            user.Positions.Add(position);
            position.Name = "Projektleder";
            position.OrgUnitUuid = projects.Uuid;
            userService.Update(user);
        }

        private static void TestUpdateWithoutChanges()
        {
            OrgUnitRegistration orgUnitRegistration = OUReg();
            orgUnitService.Update(orgUnitRegistration);
            orgUnitService.Update(orgUnitRegistration);

            UserRegistration userRegistration = UserReg();
            Position position = new Position();
            userRegistration.Positions.Add(position);
            position.Name = "PositionNameValue";
            position.OrgUnitUuid = orgUnitRegistration.Uuid;

            userService.Update(userRegistration);
            userService.Update(userRegistration);
        }

        private static void TestSecondaryPost()
        {
            string primePost = "Skovvej 21a, 8000 Aarhus C";
            string secondaryPost = "Skovvej 21b, 8000 Aarhus C";

            OrgUnitRegistration registration = OUReg();
            registration.Post = primePost;
            registration.PostSecondary = secondaryPost;
            orgUnitService.Update(registration);

            var ou = orgUnitService.Read(registration.Uuid);
            if (!primePost.Equals(ou.Post))
            {
                throw new Exception("Mismatch on prime post address. Got " + ou.Post + " expected " + primePost);
            }
            if (!secondaryPost.Equals(ou.PostSecondary))
            {
                throw new Exception("Mismatch on secondary post address. Got " + ou.PostSecondary + " expected " + secondaryPost);
            }

            registration.Post = primePost;
            registration.PostSecondary = null;
            orgUnitService.Update(registration);

            ou = orgUnitService.Read(registration.Uuid);
            if (!primePost.Equals(ou.Post))
            {
                throw new Exception("Mismatch on prime post address. Got " + ou.Post + " expected " + primePost);
            }
            if (ou.PostSecondary != null)
            {
                throw new Exception("Mismatch on secondary post address. Got " + ou.PostSecondary + " expected null");
            }

            registration.Post = secondaryPost;
            registration.PostSecondary = primePost;
            orgUnitService.Update(registration);

            ou = orgUnitService.Read(registration.Uuid);
            if (!secondaryPost.Equals(ou.Post))
            {
                throw new Exception("Mismatch on prime post address. Got " + ou.Post + " expected " + secondaryPost);
            }
            if (!primePost.Equals(ou.PostSecondary))
            {
                throw new Exception("Mismatch on secondary post address. Got " + ou.PostSecondary + " expected " + primePost);
            }
        }

        private static void TestCreateDeleteUpdateOU()
        {
            OrgUnitRegistration registration = OUReg();
            registration.Timestamp = DateTime.Now.AddMinutes(-5);
            orgUnitService.Update(registration);

            orgUnitService.Delete(registration.Uuid, DateTime.Now.AddMinutes(-3));

            registration.Timestamp = DateTime.Now.AddMinutes(-1);
            orgUnitService.Update(registration);

            var ou = inspectorService.ReadOUObject(registration.Uuid);
            ValidateOU(ou, registration);
        }

        private static void TestCreateDeleteUpdateUser()
        {
            OrgUnitRegistration parentRegistration = OUReg();
            orgUnitService.Update(parentRegistration);

            UserRegistration registration = UserReg();
            Position position = new Position();
            registration.Positions.Add(position);
            position.Name = "PositionNameValue";
            position.OrgUnitUuid = parentRegistration.Uuid;

            userService.Update(registration);
            userService.Delete(registration.Uuid, DateTime.Now.AddMinutes(-3));

            registration.Timestamp = DateTime.Now.AddMinutes(-1);
            userService.Update(registration);

            var user = inspectorService.ReadUserObject(registration.Uuid);
            ValidateUser(user, registration);
        }

        private static void TestCreateAndUpdateFullOU()
        {
            // create parent OUs
            OrgUnitRegistration parentReg = OUReg();
            orgUnitService.Update(parentReg);

            OrgUnitRegistration parentReg2 = OUReg();
            orgUnitService.Update(parentReg2);

            OrgUnitRegistration registration = OUReg();
            registration.Name = "Some Random OU Name";
            registration.ParentOrgUnitUuid = parentReg.Uuid;
            registration.ContactOpenHours = "ContactOpenHoursValue";
            registration.Ean = "EanValue";
            registration.Email = "EmailValue";
            registration.EmailRemarks = "EmailRemark";
            registration.Contact = "Contact";
            registration.PostReturn = "PostReturn";
            registration.Location = "LocationValue";
            registration.LOSShortName = "LOSShortNameValue";
            registration.PayoutUnitUuid = parentReg.Uuid;
            registration.PhoneNumber = "PhoneValue";
            registration.PhoneOpenHours = "PhoneOpenHoursValue";
            registration.Post = "PostValue";
            registration.FOA = "FOAValue";
            registration.PNR = "PNRValue";
            registration.SOR = "SORValue";
            orgUnitService.Update(registration);

            var ou = inspectorService.ReadOUObject(registration.Uuid);
            ValidateOU(ou, registration);

            registration.Name = "Some Random OU Name 2";
            registration.ParentOrgUnitUuid = parentReg2.Uuid;
            registration.ContactOpenHours = "ContactOpenHoursValue2";
            registration.Ean = "EanValue2";
            registration.Email = "EmailValue2";
            registration.EmailRemarks = "EmailOpenHoursValue2";
            registration.Contact = "ContactValue2";
            registration.PostReturn = "PostReturnValue2";
            registration.Location = "LocationValue2";
            registration.LOSShortName = "LOSShortNameValue2";
            registration.PayoutUnitUuid = parentReg2.Uuid;
            registration.PhoneNumber = "PhoneValue2";
            registration.PhoneOpenHours = "PhoneOpenHoursValue2";
            registration.Post = "PostValue2";
            registration.FOA = "FOAValue2";
            registration.PNR = "PNRValue2";
            registration.SOR = "SORValue2";
            orgUnitService.Update(registration);

            ou = inspectorService.ReadOUObject(registration.Uuid);
            ValidateOU(ou, registration);
        }

        private static void TestCreateAndUpdateFullUser()
        {
            // create parent OUs
            OrgUnitRegistration parentReg = OUReg();
            orgUnitService.Update(parentReg);

            OrgUnitRegistration parentReg2 = OUReg();
            orgUnitService.Update(parentReg2);

            OrgUnitRegistration parentReg3 = OUReg();
            orgUnitService.Update(parentReg3);

            UserRegistration registration = UserReg();
            registration.Email = "EmailValue";
            registration.Location = "LocationValue";
            registration.Person.Cpr = "0000000000";
            registration.Person.Name = "PersonNameValue";
            registration.PhoneNumber = "PhoneValue";
            registration.Landline = "LandlineValue";
            Position position = new Position();
            registration.Positions.Add(position);
            position.Name = "PositionNameValue";
            position.OrgUnitUuid = parentReg.Uuid;

            Position position2 = new Position();
            registration.Positions.Add(position2);
            position2.Name = "PositionNameValue3";
            position2.OrgUnitUuid = parentReg3.Uuid;

            registration.UserId = "UserIdValue";
            userService.Update(registration);

            var user = inspectorService.ReadUserObject(registration.Uuid);
            ValidateUser(user, registration);

            registration.Email = "EmailValue2";
            registration.Location = "LocationValue2";
            registration.Person.Cpr = "0000000001";
            registration.Person.Name = "PersonNameValue2";
            registration.PhoneNumber = "PhoneValue2";
            registration.Landline = "LandlineValue";
            position = new Position();
            registration.Positions.Clear();
            registration.Positions.Add(position);
            position.Name = "PositionNameValue2";
            position.OrgUnitUuid = parentReg2.Uuid;
            registration.UserId = "UserIdValue2";
            userService.Update(registration);

            user = inspectorService.ReadUserObject(registration.Uuid);
            ValidateUser(user, registration);
        }

        private static OrgUnitRegistration OUReg()
        {
            OrgUnitRegistration registration = new OrgUnitRegistration();
            registration.Uuid = Uuid();
            registration.Name = "Default OU Name";

            return registration;
        }

        private static UserRegistration UserReg()
        {
            UserRegistration registration = new UserRegistration();
            registration.Uuid = Uuid();
            registration.UserId = "DefaultUserID";
            registration.Person.Name = "DefaultPersonName";

            return registration;
        }

        private static string Uuid()
        {
            return Guid.NewGuid().ToString().ToLower();
        }

        private static void ValidateUser(DTO.Read.User user, UserRegistration registration)
        {
            if (!string.Equals(user.Person?.Cpr, registration.Person.Cpr))
            {
                throw new Exception("CPR is not the same");
            }

            if (!string.Equals(user.Person?.Name, registration.Person.Name))
            {
                throw new Exception("Name is not the same");
            }

            if (!string.Equals(user.UserId, registration.UserId))
            {
                throw new Exception("UserId is not the same");
            }

            foreach (var positionInOrg in user.Positions)
            {
                bool found = false;

                foreach (var positionInLocal in registration.Positions)
                {
                    if (positionInOrg.OU.Uuid.Equals(positionInLocal.OrgUnitUuid) && positionInOrg.Name.Equals(positionInLocal.Name))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception("Position in organisation '" + positionInOrg.Uuid + "' did not exist in local registration");
                }
            }

            foreach (var positionInLocal in registration.Positions)
            {
                bool found = false;

                foreach (var positionInOrg in user.Positions)
                {
                    if (positionInOrg.OU.Uuid.Equals(positionInLocal.OrgUnitUuid) && positionInOrg.Name.Equals(positionInLocal.Name))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    throw new Exception("Position in local registration '" + positionInLocal.Name + "' did not exist in organisation");
                }
            }

            // bit one sided, but probably enough for rough testing
            foreach (var address in user.Addresses)
            {
                if (address is DTO.Read.Email && !address.Value.Equals(registration.Email))
                {
                    throw new Exception("Email is not the same");
                }
                else if (address is DTO.Read.Location && !address.Value.Equals(registration.Location))
                {
                    throw new Exception("Location is not the same");
                }
                else if (address is DTO.Read.Phone && !address.Value.Equals(registration.PhoneNumber))
                {
                    throw new Exception("Phone is not the same");
                }
                else if (address is DTO.Read.Landline && !address.Value.Equals(registration.Landline))
                {
                    throw new Exception("Landline is not the same");
                }
                else if (address is DTO.Read.FMKID && !address.Value.Equals(registration.FMKID))
                {
                    throw new Exception("FMKID is not the same");
                }
            }

        }

        private static void ValidateOU(DTO.Read.OU orgUnit, OrgUnitRegistration registration)
        {
            if (!string.Equals(orgUnit.ParentOU?.Uuid, registration.ParentOrgUnitUuid))
            {
                throw new Exception("ParentOU reference is not the same");
            }

            if (!string.Equals(orgUnit.Name, registration.Name))
            {
                throw new Exception("Name is not the same");
            }

            if (!string.Equals(orgUnit.PayoutOU?.Uuid, registration.PayoutUnitUuid))
            {
                throw new Exception("PayoutUnit reference is not the same");
            }

            if (!string.Equals(orgUnit.PayoutOU?.Uuid, registration.PayoutUnitUuid))
            {
                throw new Exception("PayoutUnit reference is not the same");
            }

            // TODO: compare Tasks and ContactForTasks

            // bit one sided, but probably enough for rough testing
            foreach (var address in orgUnit.Addresses)
            {
                if (address is DTO.Read.Email && !address.Value.Equals(registration.Email))
                {
                    throw new Exception("Email is not the same");
                }
                else if (address is DTO.Read.Phone && !address.Value.Equals(registration.PhoneNumber))
                {
                    throw new Exception("Phone is not the same");
                }
                else if (address is DTO.Read.Location && !address.Value.Equals(registration.Location))
                {
                    throw new Exception("Location is not the same");
                }
                else if (address is DTO.Read.LOSShortName && !address.Value.Equals(registration.LOSShortName))
                {
                    throw new Exception("LOSShortName is not the same");
                }
                else if (address is DTO.Read.PhoneHours && !address.Value.Equals(registration.PhoneOpenHours))
                {
                    throw new Exception("PhoneHours is not the same");
                }
                else if (address is DTO.Read.EmailRemarks && !address.Value.Equals(registration.EmailRemarks))
                {
                    throw new Exception("EmailRemarks is not the same");
                }
                else if (address is DTO.Read.Contact && !address.Value.Equals(registration.Contact))
                {
                    throw new Exception("Contact is not the same");
                }
                else if (address is DTO.Read.PostReturn && !address.Value.Equals(registration.PostReturn))
                {
                    throw new Exception("PostReturn is not the same");
                }
                else if (address is DTO.Read.Ean && !address.Value.Equals(registration.Ean))
                {
                    throw new Exception("Ean is not the same");
                }
                else if (address is DTO.Read.ContactHours && !address.Value.Equals(registration.ContactOpenHours))
                {
                    throw new Exception("ContactHours is not the same");
                }
                else if (address is DTO.Read.FOA && !address.Value.Equals(registration.FOA))
                {
                    throw new Exception("FOA is not the same");
                }
                else if (address is DTO.Read.PNR && !address.Value.Equals(registration.PNR))
                {
                    throw new Exception("PNR is not the same");
                }
                else if (address is DTO.Read.SOR && !address.Value.Equals(registration.SOR))
                {
                    throw new Exception("SOR is not the same");
                }
            }
        }
    }
}
