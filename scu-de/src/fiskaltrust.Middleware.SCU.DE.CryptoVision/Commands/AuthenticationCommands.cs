using fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File;
using fiskaltrust.Middleware.SCU.DE.CryptoVision.Interop.File.Models.Parameters;

namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Models.Commands
{
    public static class AuthenticationCommands
    {
        public static TseCommand CreateAuthenticateUserTseCommand(string userId, byte[] pin)
        {
            if (pin.Length != 8)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            return new TseCommand(TseCommandCodeEnum.AuthenticateUser, 0x0000, new TseStringParameter(userId), new TseByteArrayParameter(pin));
        }

        public static TseCommand CreateLogoutTseCommand(string userId) => new TseCommand(TseCommandCodeEnum.Logout, 0x0000, new TseStringParameter(userId));

        public static TseCommand CreateUnblockUserTseCommand(string userId, byte[] puk, byte[] newPin)
        {
            if (puk.Length != 10)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            if (newPin.Length != 8)
            {
                throw new CryptoVisionProxyException(SeResult.ErrorParameterMismatch);
            }
            return new TseCommand(TseCommandCodeEnum.UnblockUser, 0x0000, new TseStringParameter(userId), new TseByteArrayParameter(puk), new TseByteArrayParameter(newPin));
        }
    }
}
