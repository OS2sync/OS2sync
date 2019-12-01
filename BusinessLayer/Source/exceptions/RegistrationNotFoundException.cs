using System;

namespace Organisation.BusinessLayer
{
    [Serializable]
    public class RegistrationNotFoundException : Exception
    {
        public RegistrationNotFoundException() : base() { }
        public RegistrationNotFoundException(string message) : base(message) { }
        public RegistrationNotFoundException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected RegistrationNotFoundException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

