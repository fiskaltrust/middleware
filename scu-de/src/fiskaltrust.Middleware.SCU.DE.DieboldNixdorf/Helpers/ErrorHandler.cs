using fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Exceptions;

namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf
{
    public static partial class ErrorHandler
    {
        public static void ThrowExceptionForMfcState(byte[] error)
        {
            if (error[0] == 0 && error[1] == 0)
            {
                return;
            }
            throw (error[0], error[1]) switch
            {
                (0x0, 0x1) => new DieboldNixdorfException("E_FAIL"),
                (0x0, 0x2) => new DieboldNixdorfException("E_POWER_FAIL"),
                (0x0, 0x3) => new DieboldNixdorfException("E_UNKNOWN_CMD"),
                (0x0, 0x4) => new DieboldNixdorfException("E_CMD_TOO_LONG "),
                (0x0, 0x5) => new DieboldNixdorfException("E_CMD_FOOTER_ERROR"),
                (0x0, 0x6) => new DieboldNixdorfException("E_CMD_WRONG_ORDER"),
                (0x0, 0x7) => new DieboldNixdorfException("E_PARA_COUNT_ERROR"),
                (0x0, 0x8) => new DieboldNixdorfException("E_PARA_VALUE_ERROR"),
                (0x1, 0x0) => new DieboldNixdorfException("E_DEVICE_NOT_INITIALIZED"),
                (0x1, 0x1) => new DieboldNixdorfException("E_TIME_NOT_SET"),
                (0x1, 0x2) => new DieboldNixdorfException("E_CERTIFICATE_EXPIRED"),
                (0x1, 0x3) => new DieboldNixdorfException("E_DEVICE_DISABLED"),
                (0x1, 0x4) => new DieboldNixdorfException("E_COMMAND_FAILED "),
                (0x1, 0x5) => new DieboldNixdorfException("E_SIGNING_SYSTEM_OPERATION_DATA_FAILED"),
                (0x1, 0x6) => new DieboldNixdorfException("E_RETRIEVE_LOG_MESSAGE_FAILED"),
                (0x1, 0x7) => new DieboldNixdorfException("E_STORAGE_FAILURE"),
                (0x1, 0x8) => new DieboldNixdorfException("E_NO_TRANSACTION"),
                (0x1, 0x9) => new DieboldNixdorfException("reserved"),
                (0x1, 0xA) => new DieboldNixdorfException("E_CLIENT_ID_NOT_SET"),
                (0x1, 0xB) => new DieboldNixdorfException("E_NO_DATA_AVAILABLE"),
                (0x1, 0xC) => new DieboldNixdorfException("E_SELF_TEST_REQUIRED"),
                (0x1, 0xD) => new DieboldNixdorfException("E_NO_LOG_MESSAGE"),
                (0x1, 0xE) => new DieboldNixdorfException("reserved"),
                (0x1, 0xF) => new DieboldNixdorfException("E_UNSUPPORTED_CRYPTO_CARD"),
                (0x1, 0x10) => new DieboldNixdorfException("E_USER_NOT_AUTHORIZED"),
                (0x1, 0x11) => new DieboldNixdorfException("E_USER_NOT_AUTHENTICATED"),
                (0x1, 0x12) => new DieboldNixdorfException("E_USER_ID_NOT_MANAGED"),
                (0x1, 0x13) => new DieboldNixdorfException("E_DESCRIPTION_NOT_SET_BY_MANUFACTURER"),
                (0x1, 0x14) => new DieboldNixdorfException("E_DESCRIPTION_SET_BY_MANUFACTURER"),
                (0x1, 0x15) => new DieboldNixdorfException("E_CLIENT_NOT_REGISTERED"),
                (0x1, 0x16) => new DieboldNixdorfException("E_CRYPTO_CARD_NOT_FOUND"),
                (0x1, 0x17) => new DieboldNixdorfException("E_MAX_PARALLEL_TRANSACTIONS_REACHED"),
                (0x1, 0x18) => new DieboldNixdorfException("E_MAX_SIGNATURES_REACHED"),
                (0x1, 0x19) => new DieboldNixdorfException("E_MAX_REGISTERED_CLIENTS_REACHED"),
                (0x1, 0x1A) => new DieboldNixdorfException("E_CLIENT_HAS_UNFINISHED_TRANSACTIONS"),
                (0x1, 0x1B) => new DieboldNixdorfException("E_DEVICE_HAS_UNFINISHED_TRANSACTIONS"),
                (0x1, 0x1C) => new DieboldNixdorfException("NotDocumented->ExportBufferCorrputed"),
                (0x1, 0x1D) => new DieboldNixdorfException("NotDocumented->???"),
                _ => new DieboldNixdorfException("Unkown error"),
            };
        }
    }
}

