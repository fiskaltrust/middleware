using System;

namespace fiskaltrust.Middleware.Contracts.Exceptions
{
    [Serializable]
    public class UnknownChargeItemException : Exception
    {
        private static readonly string _message = $"The given ChargeItemCase 0x{0:x} is not supported. Please refer to docs.fiskaltrust.cloud for supported cases.";
        public UnknownChargeItemException() { }

        public UnknownChargeItemException(long chargeItemCase) : base(string.Format(_message, chargeItemCase)) { }

    }
}
