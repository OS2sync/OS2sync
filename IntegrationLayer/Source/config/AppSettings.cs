
namespace Organisation.IntegrationLayer
{
    internal class AppSettings
    {
        public StsSettings StsSettings { get; set; } = new StsSettings();
        public ServiceSettings ServiceSettings { get; set; } = new ServiceSettings();
        public ClientSettings ClientSettings { get; set; } = new ClientSettings();
        public LogSettings LogSettings { get; set; } = new LogSettings();
        public SchedulerSettings SchedulerSettings { get; set; } = new SchedulerSettings();
        public SslSettings SslSettings { get; set; } = new SslSettings();
        public ReadSettings ReadSettings { get; set; } = new ReadSettings();

        public string Cvr { get; set; }
        public string ApiKey { get; set; }
        public string CvrUuid { get; set; }
        public bool TrustAllCertificates { get; set; } = false;
        public bool PassiverAndReImportOnErrors { get; set; } = false;
        public string Environment { get; set; } = "PROD";
        
        public bool CleanupMultiUserOrgFunctions { get; set; } = false;

        // flip to TRUE, and updates on OUs will start by wiping all existing addresses, and then creating new ones
        public bool RecreateOrgunitAddresses { get; set; } = false;

        // flip to TRUE, and updates on Brugers will start by wiping all existing addresses, and then creating new ones
        public bool RecreateBrugerAddresses { get; set; } = false;
    }
}
