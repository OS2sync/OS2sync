
namespace Organisation.IntegrationLayer
{
    internal class LogSettings
    {
        public string LogLevel { get; set; } = "INFO";
        public string LogFile { get; set; }
        public bool LogRequestResponse { get; set; } = false;
    }
}
