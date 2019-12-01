using System;

namespace Organisation.IntegrationLayer
{
    [Serializable]
    public class RegistryException : Exception
    {
        public RegistryException() : base() { }
        public RegistryException(string message) : base(message) { }
        public RegistryException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected RegistryException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

