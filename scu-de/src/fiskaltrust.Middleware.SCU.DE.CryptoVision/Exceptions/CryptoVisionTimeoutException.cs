using System;
using System.Runtime.Serialization;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions
{
    [Serializable]
    public class CryptoVisionTimeoutException : CryptoVisionException
    {

        public SeResult SeResult { get; private set; }

        public CryptoVisionTimeoutException() : base()
        {
            SeResult = SeResult.ErrorTSETimeout;
        }

        public CryptoVisionTimeoutException(string message) : base(message)
        {
            SeResult = SeResult.ErrorTSETimeout;
        }

        public CryptoVisionTimeoutException(string message, Exception innerException) : base(message, innerException)
        {
            SeResult = SeResult.ErrorTSETimeout;
        }

        public CryptoVisionTimeoutException(SeResult seResult) : base()
        {
            SeResult = seResult;
        }

        public CryptoVisionTimeoutException(SeResult seResult, string message) : base(message)
        {
            SeResult = seResult;
        }

        public CryptoVisionTimeoutException(SeResult seResult, string message, Exception innerException) : base(message, innerException)
        {
            SeResult = seResult;
        }

        protected CryptoVisionTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
