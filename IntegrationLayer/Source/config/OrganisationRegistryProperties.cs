using System;
using System.Collections.Generic;
using System.IO;
using log4net.Config;
using log4net;
using log4net.Layout;
using log4net.Appender;
using log4net.Core;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationRegistryProperties
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string LOG_REQUEST_RESPONSE = "LogRequestResponse";
        private const string REVOCATION_CHECK_KEY = "DisableRevocationCheck";
        private const string DB_CONNECTION_STRING_KEY = "DBConnectionString";
        private const string DB_TYPE_STRING_KEY = "DatabaseType";
        private const string CLIENTCERT_PATH_KEY = "ClientCertPath";
        private const string CLIENTCERT_PASSWORD_KEY = "ClientCertPassword";
        private const string SSL_ENABLED = "SslEnabled";
        private const string SSL_KEYSTORE_PATH = "SslKeystorePath";
        private const String SSL_KEYSTORE_PASSWORD = "SslKeystorePassword";
        private const string ENVIRONMENT_KEY = "Environment";
        private const string LOG_LEVEL_KEY = "LogLevel";
        private const string MUNICIPALITY_KEY = "Municipality";
        private const string API_KEY = "ApiKey";
        private const string ENABLE_SCHEDULER_KEY = "EnableScheduler";
        private const string DISABLE_KLE_OPGAVER = "DisableKleOpgaver";

        private static OrganisationRegistryProperties instance;

        public string ClientCertPath { get; set; }
        public string ClientCertPassword { get; set; }
        public string ServicesBaseUrl { get; set; }
        public bool LogRequestResponse { get; set; }
        public bool DisableRevocationCheck { get; set; }
        public bool EnableScheduler { get; set; }
        public Dictionary<string, string> MunicipalityOrganisationUUID { get; set; }
        public string ApiKey { get; set; }
        public string DBConnectionString { get; set; }
        public DatabaseType Database { get; set; }
        public string DefaultMunicipality { get; set; }
        public Level LogLevel { get; set;} = Level.Info; // default
        public string MigrationScriptsPath { get; set; }
        public bool DisableKleOpgaver { get; set; }
        public bool SslEnabled { get; set; }
        public string SslKeystorePath { get; set; }
        public string SslKeystorePassword { get; set; }

        [ThreadStatic]
        private static string MunicipalityThreadValue;

        private OrganisationRegistryProperties()
        {
            try
            {
                Init();
                log.Info("Loaded Registry Properties");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static OrganisationRegistryProperties GetInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            return (instance = new OrganisationRegistryProperties());
        }

        public static string GetCurrentMunicipality()
        {
            if (string.IsNullOrEmpty(MunicipalityThreadValue))
            {
                return GetInstance().DefaultMunicipality;
            }

            return MunicipalityThreadValue;
        }

        public static void SetCurrentMunicipality(string municipality)
        {
            if (!string.IsNullOrEmpty(municipality))
            {
                MunicipalityThreadValue = municipality;
            }
            else
            {
                MunicipalityThreadValue = GetInstance().DefaultMunicipality;
            }
        }

        private void Init()
        {
            var configuration = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json")
                 .AddEnvironmentVariables()
                 .Build();

            ClientCertPath = configuration[CLIENTCERT_PATH_KEY];
            ClientCertPassword = configuration[CLIENTCERT_PASSWORD_KEY];
            LogRequestResponse = "true".Equals(configuration[LOG_REQUEST_RESPONSE]);
            DBConnectionString = configuration[DB_CONNECTION_STRING_KEY];
            DisableRevocationCheck = "true".Equals(configuration[REVOCATION_CHECK_KEY]);
            DefaultMunicipality = configuration[MUNICIPALITY_KEY];
            EnableScheduler = "true".Equals(configuration[ENABLE_SCHEDULER_KEY]);
            DisableKleOpgaver = "true".Equals(configuration[DISABLE_KLE_OPGAVER]);

            SslEnabled = "true".Equals(configuration[SSL_ENABLED]);
            if (SslEnabled)
            {
                SslKeystorePath = configuration[SSL_KEYSTORE_PATH];
                SslKeystorePassword = configuration[SSL_KEYSTORE_PASSWORD];
            }

            DatabaseType type;
            Enum.TryParse(configuration[DB_TYPE_STRING_KEY], out type);
            Database = type;

            switch (Database)
            {
                case DatabaseType.MSSQL:
                    MigrationScriptsPath = Path.Combine(Directory.GetCurrentDirectory(), "mssql");
                    break;
                case DatabaseType.MYSQL:
                    MigrationScriptsPath = Path.Combine(Directory.GetCurrentDirectory(), "mysql");
                    break;
            }

            ApiKey = configuration[API_KEY];

            string logLevel = configuration[LOG_LEVEL_KEY];
            if ("DEBUG".Equals(logLevel))
            {
                LogLevel = Level.Debug;
            }
            else if ("INFO".Equals(logLevel))
            {
                LogLevel = Level.Info;
            }
            else if ("WARN".Equals(logLevel))
            {
                LogLevel = Level.Warn;
            }
            else if ("ERROR".Equals(logLevel))
            {
                LogLevel = Level.Error;
            }
            else
            {
                LogLevel = Level.Info;
            }

            InitLog();

            string environmentValue = configuration[ENVIRONMENT_KEY];
            Environment environment;
            if ("TEST".Equals(environmentValue))
            {
                environment = new TestEnvironment();
            }
            else if ("PROD".Equals(environmentValue))
            {
                environment = new ProdEnvironment();
            }
            else
            {
                throw new Exception("Environment must be PROD or TEST. Was = " + environmentValue);
            }

            ServicesBaseUrl = environment.GetServicesBaseUrl();

            log.Info("Configuration:\n"
               + "  clientCertPath: " + ClientCertPath + "\n"
               + "  logRequestResponse: " + LogRequestResponse + "\n"
               + "  municipality: " + DefaultMunicipality + "\n"
               + "  enableScheduler: " + EnableScheduler + "\n"
               + "  environment: " + environmentValue
            );

            // list of all 98 municipalities
            MunicipalityOrganisationUUID = new Dictionary<string, string>();
            MunicipalityOrganisationUUID["66137112"] = "222b598b-9b34-45c0-8c1b-723a64996ce5";
            MunicipalityOrganisationUUID["60183112"] = "8a383894-a598-44bb-be33-aad9b6e8af0b";
            MunicipalityOrganisationUUID["29189692"] = "9ec21a05-3f28-4187-a9e2-24d0a303e14c";
            MunicipalityOrganisationUUID["58271713"] = "4c5c9482-cab6-4a85-8491-88f98e61d161";
            MunicipalityOrganisationUUID["29189765"] = "9aa01f7f-3168-4191-92f2-37312dcbe9b9";
            MunicipalityOrganisationUUID["26696348"] = "b3786c0d-314a-477e-9e25-a760f8c92bc2";
            MunicipalityOrganisationUUID["65113015"] = "91814646-a7a4-4ffb-b768-ffd5b69e7f7c";
            MunicipalityOrganisationUUID["29189501"] = "f857cf98-fd08-45d3-991e-e90e20f6a5f8";
            MunicipalityOrganisationUUID["12881517"] = "70d1d147-f8bd-42a2-bb15-d9103c95181f";
            MunicipalityOrganisationUUID["29188386"] = "1af83689-a59a-4d4a-bb59-fef4e5709e4d";
            MunicipalityOrganisationUUID["29189803"] = "09a8b0a7-4de9-469b-b096-eae655acc2cf";
            MunicipalityOrganisationUUID["31210917"] = "0b34d992-53fb-4f2a-9643-5752b81e5867";
            MunicipalityOrganisationUUID["29189714"] = "74b6fa16-71ce-4f94-b808-b243042d379b";
            MunicipalityOrganisationUUID["29188475"] = "7e174e18-7404-4912-91dc-928039d36aa6";
            MunicipalityOrganisationUUID["29188335"] = "3105ed3e-afb0-4bb5-a957-5b34287921d9";
            MunicipalityOrganisationUUID["69116418"] = "2e52cf5d-e9bf-4609-9df2-3dfac964e17e";
            MunicipalityOrganisationUUID["11259979"] = "ea5295c9-11b9-4a1d-ad6d-033b2c581010";
            MunicipalityOrganisationUUID["29189498"] = "4939b215-defc-4a1a-8bfc-0a5bc0422929";
            MunicipalityOrganisationUUID["29189129"] = "2f37a412-9893-4ac3-841e-03f0452ae0ff";
            MunicipalityOrganisationUUID["29188327"] = "41e13528-0dea-4734-87b7-107dc498e009";
            MunicipalityOrganisationUUID["29188645"] = "1622cdf6-3692-4909-86a0-797b7df25aee";
            MunicipalityOrganisationUUID["19438414"] = "e851ea31-98fb-4383-8831-7a4f8192bcbb";
            MunicipalityOrganisationUUID["62761113"] = "2ab2488e-29a0-4ade-a21e-85357055269b";
            MunicipalityOrganisationUUID["65120119"] = "8aefa252-228c-4e23-803e-a8f7505e48fc";
            MunicipalityOrganisationUUID["44023911"] = "6c3e72fe-0bcc-4801-b8da-c0cee2efb10f";
            MunicipalityOrganisationUUID["29188440"] = "ae5c1447-b025-403d-9291-d1b08c88b87f";
            MunicipalityOrganisationUUID["29188599"] = "1fd139ec-262e-46db-8464-a61af8c18ffa";
            MunicipalityOrganisationUUID["29189757"] = "5c3ee0d8-418a-48bf-9c2f-f97e80798182";
            MunicipalityOrganisationUUID["29188416"] = "a96c50c2-9696-41de-b461-c828f36248f2";
            MunicipalityOrganisationUUID["29189587"] = "fa39a2af-3ed4-4ee0-95ac-4bd80d39d837";
            MunicipalityOrganisationUUID["64502018"] = "756c5939-e42e-4fd9-8228-5193306b9c15";
            MunicipalityOrganisationUUID["63640719"] = "f99c9bbe-e3ee-4a66-8b0e-4aece05883b2";
            MunicipalityOrganisationUUID["29189919"] = "9a7184be-e99e-484f-975d-4b1be71271b5";
            MunicipalityOrganisationUUID["29189366"] = "ed3a1e8f-01f0-4527-bab9-4fe9ff651727";
            MunicipalityOrganisationUUID["29189382"] = "6fb02f8f-bf63-4fd2-8f52-23e54aa193c9";
            MunicipalityOrganisationUUID["29189447"] = "53eea4fa-dfc6-4193-b598-8016076c7d43";
            MunicipalityOrganisationUUID["29189927"] = "0e582add-7b4e-4c66-b12b-5c10a70d1d18";
            MunicipalityOrganisationUUID["29189889"] = "c17b8cfa-5101-4bd5-8149-ade8d4524121";
            MunicipalityOrganisationUUID["55606617"] = "152caa84-34fb-4680-b5c5-3a06439fa848";
            MunicipalityOrganisationUUID["19501817"] = "5070835f-7845-49f1-aa9d-d530da9f4689";
            MunicipalityOrganisationUUID["70960516"] = "7cd72d16-0b49-4e00-8518-4f68f0727f8b";
            MunicipalityOrganisationUUID["29189617"] = "effeda8d-7f3a-4b58-af9c-9bcece056a61";
            MunicipalityOrganisationUUID["11931316"] = "32954aa8-d1fc-4813-acea-b5eb3007be2b";
            MunicipalityOrganisationUUID["29189439"] = "f329f8d3-b334-4dc7-8469-7362d66e2390";
            MunicipalityOrganisationUUID["29189595"] = "d153ae2c-f342-403b-9f48-f09ccd5858f7";
            MunicipalityOrganisationUUID["29189706"] = "4a45c56d-0537-48c9-a48b-8fcd01a89c83";
            MunicipalityOrganisationUUID["29189897"] = "c9446da2-2410-42f5-9987-158b76e9cd3d";
            MunicipalityOrganisationUUID["64942212"] = "9609b014-558d-4a45-b1c1-f37a7626bbc1";
            MunicipalityOrganisationUUID["29189374"] = "dab2b985-3064-4fa9-9924-7159fbd06324";
            MunicipalityOrganisationUUID["29188955"] = "332e1b33-0eb8-45e2-a178-069afdd703a6";
            MunicipalityOrganisationUUID["29188548"] = "f433c42d-6663-41be-b909-2807803287b9";
            MunicipalityOrganisationUUID["29189935"] = "6153c4cb-c7a1-4722-87d8-f6bddb7b2e1a";
            MunicipalityOrganisationUUID["29188572"] = "808df8d2-2e13-4904-a03e-0a9a2072703b";
            MunicipalityOrganisationUUID["11715311"] = "a6bbff5d-3fd0-4c1e-b4da-37382f189619";
            MunicipalityOrganisationUUID["45973328"] = "4c605f44-99b7-46c3-bf1f-f76c9e609883";
            MunicipalityOrganisationUUID["29189455"] = "ca43e34d-78ce-4d82-8f32-4ed805809529";
            MunicipalityOrganisationUUID["29189684"] = "2fd5a525-b9af-406c-b1ce-e899427df88b";
            MunicipalityOrganisationUUID["41333014"] = "b49165fa-b0b2-456d-ac40-82913bad3608";
            MunicipalityOrganisationUUID["29189986"] = "838d9455-19a4-437c-9ad1-05d4db487c50";
            MunicipalityOrganisationUUID["29188947"] = "a437b6c1-6a5a-429e-a7dd-90ac06fff597";
            MunicipalityOrganisationUUID["29189722"] = "35640127-cc20-40d6-ab8a-e00f4d34547b";
            MunicipalityOrganisationUUID["29189625"] = "a18014ce-f5cb-41bd-9ed8-5f68f6cd4aa9";
            MunicipalityOrganisationUUID["32264328"] = "59fe5f53-0edc-4da8-b90b-5acac63daba4";
            MunicipalityOrganisationUUID["35209115"] = "76b8ef41-7e58-4d29-b2e9-f8c72f89eb7d";
            MunicipalityOrganisationUUID["29188459"] = "5ca90290-b772-4ee0-9111-0dc04267be2c";
            MunicipalityOrganisationUUID["29189668"] = "f9591e48-dc36-4deb-a1c6-5c8270bbea8c";
            MunicipalityOrganisationUUID["29189463"] = "f4455242-14be-45ab-a778-09fdc899a183";
            MunicipalityOrganisationUUID["29189609"] = "59dca725-b69c-4a36-9562-67cb5967ee60";
            MunicipalityOrganisationUUID["18957981"] = "67831b6a-cb27-465e-af32-59b37c6fed58";
            MunicipalityOrganisationUUID["29189404"] = "603b0483-3af6-4bb1-89bc-bd2b4311edcd";
            MunicipalityOrganisationUUID["29188378"] = "8023b040-87be-4b45-8b81-f04ce6fc0316";
            MunicipalityOrganisationUUID["65307316"] = "1b99c7fe-4235-499a-b3f8-3b2546a50a9f";
            MunicipalityOrganisationUUID["23795515"] = "e412681a-7eff-4d94-b448-241719b04566";
            MunicipalityOrganisationUUID["29189641"] = "b87dae3d-d77f-4510-8ed2-6b30a1e001a9";
            MunicipalityOrganisationUUID["29189633"] = "6f8bfe01-1c1b-422d-a654-8a247fbad9b3";
            MunicipalityOrganisationUUID["29189579"] = "2d20d547-b06f-40c6-b563-48a7658195ef";
            MunicipalityOrganisationUUID["29188505"] = "eee4a771-6923-4725-81a2-cf5c2c77b4c7";
            MunicipalityOrganisationUUID["68534917"] = "adb73d6c-3306-414e-9f3a-a0a955604c22";
            MunicipalityOrganisationUUID["29189994"] = "87bfea23-19ab-4801-8736-d29cf6dcb60a";
            MunicipalityOrganisationUUID["29208654"] = "f765a798-424c-4ef6-af4c-601b7d8038d0";
            MunicipalityOrganisationUUID["29189951"] = "1ec0f5bb-53a3-4440-b0f9-079e776c701f";
            MunicipalityOrganisationUUID["29189730"] = "30bf5edf-70af-45eb-a364-fb3b424a09b0";
            MunicipalityOrganisationUUID["29189978"] = "89cba575-d106-4732-87ab-45c09ac56874";
            MunicipalityOrganisationUUID["29189773"] = "c1accb2a-210b-40b9-9b5b-b971656638f0";
            MunicipalityOrganisationUUID["29189560"] = "7c3f9da3-3814-4fd3-94db-eba3e951e061";
            MunicipalityOrganisationUUID["29189781"] = "9efeeeec-f479-4146-a90b-a77a319c1e50";
            MunicipalityOrganisationUUID["20310413"] = "af084a44-b4e8-4541-ad4a-0699d8837cca";
            MunicipalityOrganisationUUID["19583910"] = "42dd1e2e-37c5-4ef3-88cc-6179efafa1ac";
            MunicipalityOrganisationUUID["29189811"] = "74727aba-97bb-4c0b-b4c8-192dde5cb633";
            MunicipalityOrganisationUUID["29189838"] = "bae66087-307b-47a1-b777-4c7fabc0116c";
            MunicipalityOrganisationUUID["29189900"] = "f68b0a32-0ae9-4adf-ab96-463c6cae3276";
            MunicipalityOrganisationUUID["29189471"] = "97ee11f4-935c-42a8-8ddc-07d49c9606a9";
            MunicipalityOrganisationUUID["29189846"] = "f79ea045-08ae-4d3a-86b6-10d29a001ba2";
            MunicipalityOrganisationUUID["29189676"] = "a3a0e380-c63e-44de-a471-c27a6cef3400";
            MunicipalityOrganisationUUID["28856075"] = "9a545b77-a729-4b10-80be-d8b0e5f87353";
            MunicipalityOrganisationUUID["29189854"] = "46844182-e850-4f9f-a95e-5d8fb3c45520";
            MunicipalityOrganisationUUID["29189420"] = "90b865ac-40b7-47ab-a625-06cbec45ec95";
            MunicipalityOrganisationUUID["55133018"] = "43e416bd-13ac-4d76-9f98-e2c963ffe3f7";
        }

        private void InitLog()
        {
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "log.config")))
            {
                var logRepository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());

                XmlConfigurator.ConfigureAndWatch(logRepository, new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "log.config")));
            }
            else
            {
                PatternLayout patternLayout = new PatternLayout();
                patternLayout.ConversionPattern = "%date - %-5level %logger - %message%newline";
                patternLayout.ActivateOptions();

                ConsoleAppender appender = new ConsoleAppender();
                appender.Layout = patternLayout;
                appender.ActivateOptions();

                var logRepository = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository(Assembly.GetEntryAssembly());
                logRepository.Root.AddAppender(appender);

                logRepository.Root.Level = LogLevel;
                logRepository.Configured = true;
            }
        }
    }
}
