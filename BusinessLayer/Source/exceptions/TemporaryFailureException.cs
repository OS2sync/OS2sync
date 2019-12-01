using System;

namespace Organisation.BusinessLayer
{
    [Serializable]
    public class TemporaryFailureException : Exception
    {
        public TemporaryFailureException() : base() { }
        public TemporaryFailureException(string message) : base(message) { }
        public TemporaryFailureException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected TemporaryFailureException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

