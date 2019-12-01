using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Organisation.IntegrationLayer
{
    internal class LoggingBehavior : IEndpointBehavior
    {
        private string serviceName;
        private string operation;

        public LoggingBehavior(string serviceName, string operation)
        {
            this.serviceName = serviceName;
            this.operation = operation;
        }

        public void Validate(ServiceEndpoint endpoint)
        {
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(new LoggingMessageInspector(serviceName, operation));
        }
    }
}
