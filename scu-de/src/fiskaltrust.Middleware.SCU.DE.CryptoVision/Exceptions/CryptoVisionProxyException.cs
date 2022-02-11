using System;
using System.Runtime.Serialization;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions
{
    [Serializable]
    public class CryptoVisionProxyException : CryptoVisionException
    {

        public SeResult SeResult { get; private set; }

        public CryptoVisionProxyException() : base()
        {
            SeResult = SeResult.ErrorUnknown;
        }

        public CryptoVisionProxyException(string message) : base(message)
        {
            SeResult = SeResult.ErrorUnknown;
        }

        public CryptoVisionProxyException(string message, Exception innerException) : base(message, innerException)
        {
            SeResult = SeResult.ErrorUnknown;
        }

        public CryptoVisionProxyException(SeResult seResult) : base()
        {
            SeResult = seResult;
        }

        public CryptoVisionProxyException(SeResult seResult, string message) : base(message)
        {
            SeResult = seResult;
        }

        public CryptoVisionProxyException(SeResult seResult, string message, Exception innerException) : base(message, innerException)
        {
            SeResult = seResult;
        }

        protected CryptoVisionProxyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
