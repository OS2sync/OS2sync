using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace Organisation.IntegrationLayer
{
    internal class LoggingMessageInspector : IClientMessageInspector
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private string serviceName;
        private string operation;

        public LoggingMessageInspector(string serviceName, string operation)
        {
            this.serviceName = serviceName;
            this.operation = operation;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            Log(ref reply, "(" + serviceName + "." + operation + ") Reply: ");
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            Log(ref request, "(" + serviceName + "." + operation + ") Request: ");

            return null;
        }

        private void Log(ref Message message, string prefix)
        {
            // copy into buffer (and recreate message, because it is lost when creating buffered copy)
            MessageBuffer buffer = message.CreateBufferedCopy(Int32.MaxValue);
            message = buffer.CreateMessage();

            // convert message to string
            Message msg = buffer.CreateMessage();
            StringBuilder sb = new StringBuilder();
            XmlWriter xw = XmlWriter.Create(sb);
            msg.WriteBody(xw);
            xw.Close();

            log.Info(prefix + sb.ToString());
        }
    }
}
