using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop;
using fiskaltrust.Middleware.SCU.DE.Swissbit.Interop.Exceptions;

namespace fiskaltrust.Middleware.SCU.DE.Swissbit.Exceptions
{
    public static class SwissbitExceptionHelper
    {
        public static void ThrowIfError(this NativeFunctionPointer.WormError error)
        {
            switch (error)
            {
                case NativeFunctionPointer.WormError.WORM_ERROR_NOERROR:
                    break;
                case NativeFunctionPointer.WormError.WORM_ERROR_INVALID_PARAMETER:
                    throw new SwissbitException("Invalid input parameter.", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NO_WORM_CARD:
                    throw new SwissbitException("No TSE was found at the provided path.", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_IO:
                    throw new SwissbitException("IO Error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TIMEOUT:
                    throw new SwissbitException("Operation timed out. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_OUTOFMEM:
                    throw new SwissbitException("Out of memory. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_INVALID_RESPONSE:
                    throw new SwissbitException("Invalid Response from TSE. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_STORE_FULL_INTERNAL:
                    throw new SwissbitException("The TSE Store is full. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_RESPONSE_MISSING:
                    throw new SwissbitException("A command was not acknowledged", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_EXPORT_NOT_INITIALIZED:
                    throw new SwissbitException("TSE not initialized. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_EXPORT_FAILED:
                    throw new SwissbitException("Export Failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_INCREMENTAL_EXPORT_INVALID_STATE:
                    throw new SwissbitException("Incremental Export: invalid state.", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_INCREMENTAL_EXPORT_NO_DATA:
                    throw new SwissbitException("Incremental Export: no new data. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_POWER_CYCLE_DETECTED:
                    throw new SwissbitException("A power cycle occurred during command execution. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FIRMWARE_UPDATE_NOT_APPLIED:
                    throw new SwissbitException("The firmware update was not properly applied. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_THREAD_START_FAILED:
                    throw new SwissbitException("Failed to start the background thread for keeping the TSE awake. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FROM_CARD_FIRST:
                    throw new SwissbitException("Lowest error code that might be raised from the TSE. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_UNKNOWN:
                    throw new SwissbitException("Unspecified, internal processing error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NO_TIME_SET:
                    throw new SwissbitException("Time not set. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NO_TRANSACTION_IN_PROGRESS:
                    throw new SwissbitException("No transaction in progress. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_INVALID_CMD_SYNTAX:
                    throw new SwissbitException("Wrong command length. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NOT_ENOUGH_DATA_WRITTEN:
                    throw new SwissbitException("Not enough data written during transaction. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TSE_INVALID_PARAMETER:
                    throw new SwissbitException("Invalid Parameter. ");
                case NativeFunctionPointer.WormError.WORM_ERROR_TRANSACTION_NOT_STARTED:
                    throw new SwissbitException("Given transaction is not started. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_MAX_PARALLEL_TRANSACTIONS:
                    throw new SwissbitException("Maximum parallel transactions reached. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_CERTIFICATE_EXPIRED:
                    throw new SwissbitException("Certificate expired. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NO_LAST_TRANSACTION:
                    throw new SwissbitException("No last transaction to fetch. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_CMD_NOT_ALLOWED:
                    throw new SwissbitException("Command not allowed in current state. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TRANSACTION_SIGNATURES_EXCEEDED:
                    throw new SwissbitException("Signatures exceeded. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NOT_AUTHORIZED:
                    throw new SwissbitException("Not authorized. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_MAX_REGISTERED_CLIENTS_REACHED:
                    throw new SwissbitException("Maximum registered clients reached. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_CLIENT_NOT_REGISTERED:
                    throw new SwissbitException("Client not registered. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_EXPORT_UNACKNOWLEDGED_DATA:
                    throw new SwissbitException("Failed to delete, data not completely exported. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_CLIENT_HAS_UNFINISHED_TRANSACTIONS:
                    throw new SwissbitException("Failed to deregister, client has unfinished transactions. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TSE_HAS_UNFINISHED_TRANSACTIONS:
                    throw new SwissbitException("Failed to decommission, TSE has unfinished transactions. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TSE_NO_RESPONSE_TO_FETCH:
                    throw new SwissbitException("Wrong state, there is no response to fetch. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_NOT_ALLOWED_EXPORT_IN_PROGRESS:
                    throw new SwissbitException("Wrong state, ongoing Filtered Export must be finished before this command is allowed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_STORE_FULL:
                    throw new SwissbitException("Operation failed, not enough remaining capacity in TSE Store. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_WRONG_STATE_NEEDS_PUK_CHANGE:
                    throw new SwissbitException("Wrong state, changed PUK required. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_WRONG_STATE_NEEDS_PIN_CHANGE:
                    throw new SwissbitException("Wrong state, changed PIN required. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_WRONG_STATE_NEEDS_ACTIVE_CTSS:
                    throw new SwissbitException("Wrong state, active CTSS interface required. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_WRONG_STATE_NEEDS_SELF_TEST:
                    throw new SwissbitException("Wrong state, self test must be run first. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_WRONG_STATE_NEEDS_SELF_TEST_PASSED:
                    throw new SwissbitException("Wrong state, passed self test required. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_INTEGRITY_FAILURE:
                    throw new SwissbitException("Firmware Update: Integrity check failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_DECRYPTION_FAILURE:
                    throw new SwissbitException("Firmware Update: Decryption failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_WRONG_FORMAT:
                    throw new SwissbitException("Firmware Update: Wrong format. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_INTERNAL_ERROR:
                    throw new SwissbitException("Firmware Update: Internal error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_DOWNGRADE_PROHIBITED:
                    throw new SwissbitException("Firmware Update: downgrade prohibited. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TSE_ALREADY_INITIALIZED:
                    throw new SwissbitException("TSE already initialized. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TSE_DECOMMISSIONED:
                    throw new SwissbitException("TSE decommissioned. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_TSE_NOT_INITIALIZED:
                    throw new SwissbitException("TSE not initialized. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_AUTHENTICATION_FAILED:
                    throw new SwissbitException("Authentication failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_AUTHENTICATION_PIN_BLOCKED:
                    throw new SwissbitException("PIN is blocked. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_AUTHENTICATION_USER_NOT_LOGGED_IN:
                    throw new SwissbitException("Given user is not authenticated. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_SELF_TEST_FAILED_FW:
                    throw new SwissbitException("Self test of FW failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_SELF_TEST_FAILED_CSP:
                    throw new SwissbitException("Self test of CSP failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_SELF_TEST_FAILED_RNG:
                    throw new SwissbitException("Self test of RNG failed. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_BASE_FW_ERROR:
                    throw new SwissbitException("Firmware Update: Base FW update error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_FWEXT_ERROR:
                    throw new SwissbitException("Firmware Update: FW Extension update error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FWU_CSP_ERROR:
                    throw new SwissbitException("Firmware Update: CSP update error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_EXPORT_NONE_IN_PROGRESS:
                    throw new SwissbitException("Filtered Export: no export in progress. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_EXPORT_RETRY:
                    throw new SwissbitException("Filtered Export: no new data, keep polling. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_EXPORT_NO_DATA_AVAILABLE:
                    throw new SwissbitException("Filtered Export: no matching entries, export would be empty. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_CMD_NOT_FOUND:
                    throw new SwissbitException("Command not found. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_SIG_ERROR:
                    throw new SwissbitException("Signature creation error. ", error);
                case NativeFunctionPointer.WormError.WORM_ERROR_FROM_CARD_LAST:
                    throw new SwissbitException($"Highest error code that might be raised from the TSE. {error}", error);
                default:
                    throw new SwissbitException($"{error}", error);
            }
        }
    }
}
