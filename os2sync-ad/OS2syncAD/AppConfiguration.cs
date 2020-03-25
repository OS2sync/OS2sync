using Microsoft.Extensions.Configuration;
using System.IO;

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

        public static string DBConnectionString
        {
            get
            {
                return configuration["DBConnectionString"];
            }
        }

        public static string Cvr
        {
            get
            {
                return configuration["Municipality"];
            }
        }

        public static string RootOU
        {
            get
            {
                return configuration["AD:RootOU"];
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
