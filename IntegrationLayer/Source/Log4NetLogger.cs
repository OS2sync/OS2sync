using Digst.OioIdws.CommonCore.Logging;
using Organisation.IntegrationLayer;
using System;
using System.Diagnostics;

namespace IntegrationLayer
{
    public class Log4NetLogger : ILogger
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger("OIOIDWS");

        public void WriteCore(TraceEventType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (OrganisationRegistryProperties.AppSettings.LogSettings.LogOioidws)
            {
                switch (eventType)
                {
                    case TraceEventType.Critical:
                        Logger.Fatal(state, exception);
                        break;
                    case TraceEventType.Error:
                        Logger.Error(state, exception);
                        break;
                    case TraceEventType.Warning:
                        Logger.Warn(state, exception);
                        break;
                    case TraceEventType.Verbose:
                        Logger.Debug(state, exception);
                        break;
                    default:
                        Logger.Info(state, exception);
                        break;
                }
            }
        }
    }
}
