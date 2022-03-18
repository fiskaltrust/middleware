using System;
using System.Runtime.Serialization;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Models;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions
{
    [Serializable]
    public class CryptoVisionNotAuthenticatedException : CryptoVisionException
    {

        public SeResult SeResult { get; private set; }

        public CryptoVisionNotAuthenticatedException() : base()
        {
            SeResult = SeResult.ErrorUserNotAuthenticated;
        }

        public CryptoVisionNotAuthenticatedException(string message) : base(message)
        {
            SeResult = SeResult.ErrorUserNotAuthenticated;
        }

        public CryptoVisionNotAuthenticatedException(string message, Exception innerException) : base(message, innerException)
        {
            SeResult = SeResult.ErrorUserNotAuthenticated;
        }

        public CryptoVisionNotAuthenticatedException(SeResult seResult) : base()
        {
            SeResult = seResult;
        }

        public CryptoVisionNotAuthenticatedException(SeResult seResult, string message) : base(message)
        {
            SeResult = seResult;
        }

        public CryptoVisionNotAuthenticatedException(SeResult seResult, string message, Exception innerException) : base(message, innerException)
        {
            SeResult = seResult;
        }

        protected CryptoVisionNotAuthenticatedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
