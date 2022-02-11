namespace fiskaltrust.Middleware.SCU.DE.DieboldNixdorf.Models.Enums
{
    public enum DieboldNixdorfCommand
    {
        None = -1,
        // Standard Commands
        GetCountryInfo = 0,
        GetMfcStatus = 1,
        GetFirmwareInfo = 2,
        SetAsb = 3,
        GetCommandResponse = 4,

        // Maintenance Commands
        Initialize = 100,
        UpdateTime = 101,
        GetMemoryInfo = 102,
        StartFwUpdate = 103,
        Disable = 104,
        // 105?
        RegisterClient = 106,
        DeregisterClient = 107,
        GetDeviceInfo = 108,
        GetSlotInfo = 109,
        EnterMaintenanceMode = 110,
        LeaveMaintenanceMode = 111,
        RunSelfTest = 112,

        // Transaction Commands
        StartTransactionInit = 200,
        StartTransactionEnd = 201,
        UpdateTransactionInit = 202,
        UpdateTransactionEnd = 203,
        FinishTransactionInit = 204,
        FinishTransactionEnd = 205,
        TransactionData = 206,
        TransactionVoid = 207,

        // Export Commands
        ExportAll = 300,
        ExportByTransactionNo = 301,
        ExportByTransactionNoInterval = 302,
        ExportByTimePeriod = 303,
        ExportCertificates = 304,
        ExportSerialNumbers = 305,
        ExportLastLogMessage = 306,
        DeleteExportedData = 307,
        ExportPublicKey = 308,
        ExportWeird_NoClueWhatThisIsUnDocumented1 = 309, // 2020-10-06 Stefan Kert: This Command is not documented, but used by the Webservice. It looks like it can be used for reseting the export if it crashed and brought the TSE into an invalid state
        
        // Authentication Commands
        LoginUser = 400,
        LogoutUser = 401,
        Unblockuser = 402,
        ChangePuk = 403,
        ChangePin = 404,

        // Utility Commands
        GetNumberOfClients = 500,
        GetRegisteredClients = 502,
        GetSupportedTransactionUpdateVariants = 503,
        GetNumberOfTransactions = 504,
        GetStartedTransactions = 506,
        GetUpdateTimeInterval = 507,
        GetTimeUntilNextSelfTest = 508,
        SetClientId = 509,

        // Developer Commands
        FactoryReset = 900,
    }
}