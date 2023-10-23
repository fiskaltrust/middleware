namespace fiskaltrust.Middleware.SCU.DE.CryptoVision.Exceptions
{
    public static class CryptoVisionExceptionHelper
    {
        public static void ThrowIfError(this Models.SeResult error)
        {
            switch (error)
            {
                case Models.SeResult.ExecutionOk:
                    break;
                case Models.SeResult.ErrorMissingParameter:
                    throw new CryptoVisionException("a mandatory input parameter is NULL", error);
                case Models.SeResult.ErrorFunctionNotSupported:
                    throw new CryptoVisionException("the function is not supported", error);
                case Models.SeResult.ErrorAllocationfailed:
                    throw new CryptoVisionException("the memory allocation for an output parameter failed", error);
                case Models.SeResult.ErrorFileNotFound:
                    throw new CryptoVisionException("transport I/O configuration file not found", error);
                case Models.SeResult.ErrorSECommunicationFailed:
                    throw new CryptoVisionException("communication with Secure Element failed", error);
                case Models.SeResult.ErrorTSECommandDataInvalid:
                    throw new CryptoVisionException("invalid TSE command data", error);
                case Models.SeResult.ErrorTSEResponseDataInvalid:
                    throw new CryptoVisionException("invalid TSE response data", error);
                case Models.SeResult.ErrorTSEUnknownError:
                    throw new CryptoVisionException("unknown TSE error code", error);
                case Models.SeResult.ErrorStreamWrite:
                    throw new CryptoVisionException("write to output stream failed", error);
                case Models.SeResult.ErrorIO:
                    throw new CryptoVisionException("transport I/O connection error", error);
                case Models.SeResult.ErrorTSETimeout:
                    throw new CryptoVisionTimeoutException("transport I/O timeout error");
                case Models.SeResult.ErrorBufferTooSmall:
                    throw new CryptoVisionException("transport I/O buffer too small", error);
                case Models.SeResult.ErrorCallback:
                    throw new CryptoVisionException("callback function failed (e.g. se_writer_t export data callback)", error);
                case Models.SeResult.ErrorTSEFirmwareVersion:
                    throw new CryptoVisionException("TSE firmware incompatible with API version.", error);
                case Models.SeResult.ErrorTransport:
                    throw new CryptoVisionException("data fragmentation on transport layer", error);
                case Models.SeResult.ErrorNoStartup:
                    throw new CryptoVisionException("TSE start command no called.", error);
                case Models.SeResult.ErrorNoStorage:
                    throw new CryptoVisionException("Transaction operation not allowed due to low log memory or no more ERS mappings available.", error);
                case Models.SeResult.ErrorAuthenticationFailed:
                    throw new CryptoVisionException("return value for the SE API function authenticateUser, authentication failed", error);
                case Models.SeResult.ErrorUnblockFailed:
                    throw new CryptoVisionException("return value for the SE API function unblockUser, unblock failed", error);
                case Models.SeResult.ErrorRetrieveLogMessageFailed:
                    throw new CryptoVisionException("the retrieving of the log message parts that have been created by Secure Element most recently failed", error);
                case Models.SeResult.ErrorStorageFailure:
                    throw new CryptoVisionException("storing of the log message in the storage failed", error);
                case Models.SeResult.ErrorUpdateTimeFailed:
                    throw new CryptoVisionException("the execution of the Secure Element functionality for setting the time failed", error);
                case Models.SeResult.ErrorParameterMismatch:
                    throw new CryptoVisionException("there is a mismatch regarding the particular parameters that have been provided in the context of the export of stored data", error);
                case Models.SeResult.ErrorIdNotFound:
                    throw new CryptoVisionException("no data has been found for the provided clientID in the context of the export of stored data", error);
                case Models.SeResult.ErrorNoSuchKey:
                    throw new CryptoVisionException("unknown key serial number (hash of public key)", error);
                case Models.SeResult.ErrorERSalreadyMapped:
                    throw new CryptoVisionException("the serial number of an ERS is already mapped to a signature key", error);
                case Models.SeResult.ErrorNoERS:
                    throw new CryptoVisionException("unknown ERS", error);
                case Models.SeResult.ErrorNoKey:
                    throw new CryptoVisionException("unknown signature key", error);
                case Models.SeResult.ErrorTransactionNumberNotFound:
                    throw new CryptoVisionException("no data has been found for the provided transaction number(s) in the context of the export of stored data", error);
                case Models.SeResult.ErrorNoDataAvailable:
                    throw new CryptoVisionException("no data has been found for the provided selection in the context of the export of stored data", error);
                case Models.SeResult.ErrorTooManyRecords:
                    throw new CryptoVisionException("the amount of requested records exceeds the passed value for the maximum number of records in the context of the export of stored data", error);
                case Models.SeResult.ErrorStartTransactionFailed:
                    throw new CryptoVisionException("the execution of the Secure Element functionality to start a transaction failed", error);
                case Models.SeResult.ErrorUpdateTransactionFailed:
                    throw new CryptoVisionException("the execution of the Secure Element functionality for updating a transaction failed", error);
                case Models.SeResult.ErrorFinishTransactionFailed:
                    throw new CryptoVisionException("the execution of the Secure Element functionality for finishing a transaction failed", error);
                case Models.SeResult.ErrorRestoreFailed:
                    throw new CryptoVisionException("the restore process in the context of a restoring from a backup in form of exported data failed", error);
                case Models.SeResult.ErrorStoringInitDataFailed:
                    throw new CryptoVisionException("the storing of the initialization data during the commissioning of the SE API by the application operator failed", error);
                case Models.SeResult.ErrorExportCertFailed:
                    throw new CryptoVisionException("the collection of the certificates for the export failed", error);
                case Models.SeResult.ErrorNoLogMessage:
                    throw new CryptoVisionException("no log message parts have been found in the Secure Element", error);
                case Models.SeResult.ErrorReadingLogMessage:
                    throw new CryptoVisionException("the retrieving of the log message parts that have been created from Secure Element most recently failed", error);
                case Models.SeResult.ErrorNoTransaction:
                    throw new CryptoVisionException("no transaction is known to be open under the provided transaction number", error);
                case Models.SeResult.ErrorSeApiNotInitialized:
                    throw new CryptoVisionException("the SE has not been initialized", error);
                case Models.SeResult.ErrorSeApiDeactivated:
                    throw new CryptoVisionException("the SE is temporary deactivated", error);
                case Models.SeResult.ErrorSeApiNotDeactivated:
                    throw new CryptoVisionException("the SE is not deactivated", error);
                case Models.SeResult.ErrorTimeNotSet:
                    throw new CryptoVisionException("the managed data/time in the Secure Element has not been updated after the initialization of the SE API or a period of absence of current for the Secure Element", error);
                case Models.SeResult.ErrorCertificateExpired:
                    throw new CryptoVisionException("the certificate with the public key for the verification of the appropriate type of log messages is expired", error);
                case Models.SeResult.ErrorSecureElementDisabled:
                    throw new CryptoVisionException("SE API functions are invoked although the Secure Element has been disabled.", error);
                case Models.SeResult.ErrorUserNotAuthorized:
                    throw new CryptoVisionException("the user who has invoked a restricted SE API function is not authorized to execute this function", error);
                case Models.SeResult.ErrorUserNotAuthenticated:
                    throw new CryptoVisionNotAuthenticatedException("the user who has invoked a restricted SE API function has not the status \"authenticated\"");
                case Models.SeResult.ErrorDescriptionNotSetByManufacturer:
                    throw new CryptoVisionException("the function initialize has been invoked without a value for the input parameter description although the description of the SE API has not been set by the manufacturer", error);
                case Models.SeResult.ErrorDescriptionSetByManufacturer:
                    throw new CryptoVisionException("the function initialize has been invoked with a value for the input parameter description although the description of the SE API has been set by the manufacturer", error);
                case Models.SeResult.ErrorExportSerialNumbersFailed:
                    throw new CryptoVisionException("collection of the serial number(s) failed", error);
                case Models.SeResult.ErrorGetMaxNumberOfClientsFailed:
                    throw new CryptoVisionException("determination of the maximum number of clients that could use the SE API simultaneously failed", error);
                case Models.SeResult.ErrorGetCurrentNumberOfClientsFailed:
                    throw new CryptoVisionException("determination of the current number of clients using the SE API failed", error);
                case Models.SeResult.ErrorGetMaxNumberTransactionsFailed:
                    throw new CryptoVisionException("determination of the maximum number of transactions that can be managed simultaneously failed", error);
                case Models.SeResult.ErrorGetCurrentNumberOfTransactionsFailed:
                    throw new CryptoVisionException("determination of the number of currently opened transactions failed", error);
                case Models.SeResult.ErrorGetSupportedUpdateVariantsFailed:
                    throw new CryptoVisionException("identification of the supported variant(s) for updating transactions failed", error);
                case Models.SeResult.ErrorDeleteStoredDataFailed:
                    throw new CryptoVisionException("deletion of the data from the storage failed", error);
                case Models.SeResult.ErrorUnexportedStoredData:
                    throw new CryptoVisionException("deletion of data from the storage failed because the storage contains data that has not been exported", error);
                case Models.SeResult.ErrorSigningSystemOperationDataFailed:
                    throw new CryptoVisionException("determination of the log message parts for the system operation data by the Secure Element failed", error);
                case Models.SeResult.ErrorUserIdNotManaged:
                    throw new CryptoVisionException("userId is not managed by the SE API", error);
                case Models.SeResult.ErrorUserIdNotAuthenticated:
                    throw new CryptoVisionException("userId has not the status authenticated", error);
                case Models.SeResult.ErrorDisableSecureElementFailed:
                    throw new CryptoVisionException("deactivation of the Secure Element failed", error);
                case Models.SeResult.ErrorClassSD:
                    throw new CryptoVisionException($"{error} indicate unexpected errors from SD layer", error);
                case Models.SeResult.ErrorUnknown:
                default:
                    throw new CryptoVisionException($"{error}", error);
            }

        }

    }
}
