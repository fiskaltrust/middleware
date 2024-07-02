using System;
using System.Runtime.Serialization;

namespace fiskaltrust.Middleware.SCU.IT.CustomRTPrinter.Clients
{
    [Serializable]
    internal class CustomRTPrinterException : Exception
    {
        public CustomRTPrinterException()
        {
        }

        public CustomRTPrinterException(string message) : base(message)
        {
        }

        public CustomRTPrinterException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CustomRTPrinterException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}