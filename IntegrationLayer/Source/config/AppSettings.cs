
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

        public string Cvr { get; set; }
        public string ApiKey { get; set; }
        public string CvrUuid { get; set; }
        public bool TrustAllCertificates { get; set; } = false;
        public string Environment { get; set; } = "PROD";
    }
}
