﻿namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums
{
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum DieboldNixdorfError : long
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        E_FAIL = 0x0000_0000_0001,
        E_POWER_FAIL = 0x0000_0000_0002,
        E_UNKNOWN_CMD = 0x0000_0000_0003,
        E_CMD_TOO_LONG = 0x0000_0000_0004,
        E_CMD_FOOTER_ERROR = 0x0000_0000_0005,
        E_CMD_WRONG_ORDER = 0x0000_0000_0006,
        E_PARA_COUNT_ERROR = 0x0000_0000_0007,
        E_PARA_VALUE_ERROR = 0x0000_0000_0008,
        E_DEVICE_NOT_INITIALIZED = 0x0001_0000_0000,
        E_TIME_NOT_SET = 0x0001_0000_0001,
        E_CERTIFICATE_EXPIRED = 0x0001_0000_0002,
        E_DEVICE_DISABLED = 0x0001_0000_0003,
        E_COMMAND_FAILED = 0x0001_0000_0004,
        E_SIGNING_SYSTEM_OPERATION_DATA_FAILED = 0x0001_0000_0005,
        E_RETRIEVE_LOG_MESSAGE_FAILED = 0x0001_0000_0006,
        E_STORAGE_FAILURE = 0x0001_0000_0007,
        E_NO_TRANSACTION = 0x0001_0000_0008,
        RESERVED_1 = 0x0001_0000_0009,
        E_CLIENT_ID_NOT_SET = 0x0001_0000_000A,
        E_NO_DATA_AVAILABLE = 0x0001_0000_000B,
        E_SELF_TEST_REQUIRED = 0x0001_0000_000C,
        E_NO_LOG_MESSAGE = 0x0001_0000_000D,
        RESERVED_2 = 0x0001_0000_000E,
        E_UNSUPPORTED_CRYPTO_CARD = 0x0001_0000_000F,
        E_USER_NOT_AUTHORIZED = 0x0001_0000_0010,
        E_USER_NOT_AUTHENTICATED = 0x0001_0000_0011,
        E_USER_ID_NOT_MANAGED = 0x0001_0000_0012,
        E_DESCRIPTION_NOT_SET_BY_MANUFACTURER = 0x0001_0000_0013,
        E_DESCRIPTION_SET_BY_MANUFACTURER = 0x0001_0000_0014,
        E_CLIENT_NOT_REGISTERED = 0x0001_0000_0015,
        E_CRYPTO_CARD_NOT_FOUND = 0x0001_0000_0016,
        E_MAX_PARALLEL_TRANSACTIONS_REACHED = 0x0001_0000_0017,
        E_MAX_SIGNATURES_REACHED = 0x0001_0000_0018,
        E_MAX_REGISTERED_CLIENTS_REACHED = 0x0001_0000_0019,
        E_CLIENT_HAS_UNFINISHED_TRANSACTIONS = 0x0001_0000_001A,
        E_DEVICE_HAS_UNFINISHED_TRANSACTIONS = 0x0001_0000_001B
    }
}

