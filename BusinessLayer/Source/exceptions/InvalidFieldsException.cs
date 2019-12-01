using System;

namespace Organisation.BusinessLayer
{
    [Serializable]
    public class InvalidFieldsException : Exception
    {
        public InvalidFieldsException() : base() { }
        public InvalidFieldsException(string message) : base(message) { }
        public InvalidFieldsException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client. 
        protected InvalidFieldsException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

