using System;

namespace Organisation.IntegrationLayer
{
    [Serializable]
    public class STSNotFoundException : System.Exception
    {
        public STSNotFoundException() : base() { }
        public STSNotFoundException(string message) : base(message) { }
        public STSNotFoundException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected STSNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) :base(info,context) { }

    }
}
