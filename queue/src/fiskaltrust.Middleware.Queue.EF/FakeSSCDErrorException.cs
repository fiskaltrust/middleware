using System;

namespace fiskaltrust.ifPOS.v1.errors
{
    // Fake replacement for the enum EF is choking on
    public enum SSCDErrorType
    {
        None = 0,
        Unknown = 1
        // Add other values if EF or your logic requires them
    }

    // Fake replacement for the exception
    [Serializable]
    public class SSCDErrorException : Exception
    {
        public SSCDErrorType Type { get; private set; }

        public SSCDErrorException() { }

        public SSCDErrorException(SSCDErrorType type)
        {
            Type = type;
        }
    }
}