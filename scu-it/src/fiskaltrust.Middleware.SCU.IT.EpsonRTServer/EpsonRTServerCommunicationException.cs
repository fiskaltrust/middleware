using System;

namespace fiskaltrust.Middleware.SCU.IT.EpsonRTServer
{
    public class EpsonRTServerCommunicationException : Exception
    {
        public int ResponseCode { get; }

        public EpsonRTServerCommunicationException(string message, int responseCode) : base(message)
        {
            ResponseCode = responseCode;
        }

        public EpsonRTServerCommunicationException(string message, int responseCode, Exception innerException) : base(message, innerException)
        {
            ResponseCode = responseCode;
        }
    }
}
