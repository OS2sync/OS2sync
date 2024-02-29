using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OS2syncAD
{
    class AppConfiguration
    {
        private static IConfigurationRoot configuration;

        static AppConfiguration()
        {
            configuration = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json")
                 .Build();
        }

        public static string GetOUName(string uuid)
        {
            return configuration["AD:OrgUnitNameMap:" + uuid];
        }

        public static string DBConnectionString
        {
            get
            {
                return configuration["SchedulerSettings:DBConnectionString"];
            }
        }

        public static string Cvr
        {
            get
            {
                return configuration["Cvr"];
            }
        }

        public static string RootOU
        {
            get
            {
                return configuration["AD:RootOU"];
            }
        }

        public static bool TerminateDisabledUsers
        {
            get
            {
                return "true".Equals(configuration["TerminateDisabledUsers"]);
            }
        }

        public static bool CleanupOUJobEnabled
        {
            get
            {
                return "true".Equals(configuration["CleanupOUJobEnabled"]);
            }
        }

        public static bool CleanupOUJobDryRun
        {
            get
            {
                return "true".Equals(configuration["CleanupOUJobDryRun"]);
            }
        }

        public static string CleanOUJobCron
        {
            get
            {
                return configuration["CleanupOUJobCron"];
            }
        }


        #region OU attributes

        public static string OUAttributeFiltered
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Filtered"];
            }
        }

        public static string OUAttributeEan
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Ean"];
            }
        }

        public static string OUAttributeDtrId
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:DtrId"];
            }
        }

        public static string OUAttributeEmail
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Email"];
            }
        }

        public static string OUAttributeLocation
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Location"];
            }
        }

        public static string OUAttributeLOSId
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:LOSId"];
            }
        }

        public static string OUAttributeLOSShortName
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:LOSShortName"];
            }
        }

        public static string OUAttributeName
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Name"];
            }
        }

        public static string OUAttributePayoutUnitUUID
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:PayoutUnitUuid"];
            }
        }

        public static string OUAttributePhone
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Phone"];
            }
        }

        public static string OUAttributePost
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Post"];
            }
        }

        public static string OUAttributeContact
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Contact"];
            }
        }


        public static string OUAttributeContactOpenHours
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:ContactOpenHours"];
            }
        }

        public static string OUAttributeEmailRemarks
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:EmailRemarks"];
            }
        }

        public static string OUAttributePostReturn
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:PostReturn"];
            }
        }

        public static string OUAttributePhoneOpenHours
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:PhoneOpenHours"];
            }
        }

        public static string OUAttributeUrl
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Url"];
            }
        }

        public static string OUAttributeLandline
        {
            get
            {
                return configuration["AD:OrgUnitAttributes:Landline"];
            }
        }
        #endregion

        #region User attributes

        public static string UserAttributeLocation
        {
            get
            {
                return configuration["AD:UserAttributes:Location"];
            }
        }

        public static string UserAttributeMail
        {
            get
            {
                return configuration["AD:UserAttributes:Mail"];
            }
        }

        public static string UserAttributeRacfID
        {
            get
            {
                return configuration["AD:UserAttributes:RacfID"];
            }
        }

        public static string UserAttributePersonCpr
        {
            get
            {
                return configuration["AD:UserAttributes:Cpr"];
            }
        }

        public static string UserAttributePersonName
        {
            get
            {
                return configuration["AD:UserAttributes:Name"];
            }
        }

        public static string UserAttributePhone
        {
            get
            {
                return configuration["AD:UserAttributes:Phone"];
            }
        }

        public static string UserAttributePositionName
        {
            get
            {
                return configuration["AD:UserAttributes:PositionName"];
            }
        }

        #endregion
    }
}
