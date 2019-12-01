using System;

namespace Organisation.IntegrationLayer
{
    [Serializable]
    public class ServiceNotFoundException : System.Exception
    {
        public ServiceNotFoundException() : base() { }
        public ServiceNotFoundException(string message) : base(message) { }
        public ServiceNotFoundException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected ServiceNotFoundException(System.Runtime.Serialization.SerializationInfo info,  System.Runtime.Serialization.StreamingContext context) :base(info,context) { }
    }
}
