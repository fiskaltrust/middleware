namespace fiskaltrust.Middleware.SCU.DE.Epson
{
    public static class Constants
    {
        public static class Functions
        {
            public static class Configuraton
            {
                public const string SetUp = "SetUp";
                public const string SetUpForPrinter = "SetUpForPrinter";
                public const string RunTSESelfTest = "RunTSESelfTest";
                public const string RegisterSecretKey = "RegisterSecretKey";
                public const string UpdateTime = "UpdateTime";
                public const string UpdateTimeForFirst = "UpdateTimeForFirst";
                public const string RegisterClient = "RegisterClient";
                public const string DeregisterClient = "DeregisterClient";
                public const string GetRegisteredClientList = "GetRegisteredClientList";
                public const string UnlockTSE = "UnlockTSE";
                public const string LockTSE = "LockTSE";
                public const string SetTimeOutInterval = "SetTimeOutInterval";
                public const string GetTimeOutInterval = "GetTimeOutInterval";
                public const string EnableExportIfCspTestFails = "EnableExportIfCspTestFails";
                public const string DisableExportIfCspTestFails = "DisableExportIfCspTestFails";
                public const string DisableSecureElement = "DisableSecureElement";
            }

            public static class UserAuthentication
            {
                public const string AuthenticateUserForAdmin = "AuthenticateUserForAdmin";
                public const string AuthenticateUserForTimeAdmin = "AuthenticateUserForTimeAdmin";
                public const string LogOutForAdmin = "LogOutForAdmin";
                public const string LogOutForTimeAdmin = "LogOutForTimeAdmin";
                public const string UnblockUserForAdmin = "UnblockUserForAdmin";
                public const string UnblockUserForTimeAdmin = "UnblockUserForTimeAdmin";
                public const string GetChallenge = "GetChallenge";
                public const string AuthenticateHost = "AuthenticateHost";
                public const string DeauthenticateHost = "DeauthenticateHost";
                public const string GetAuthenticatedUserList = "GetAuthenticatedUserList";
                public const string ChangePuk = "ChangePuk";
                public const string ChangePinForAdmin = "ChangePinForAdmin";
                public const string ChangePinForTimeAdmin = "ChangePinForTimeAdmin";
            }

            public static class Transaction
            {
                public const string StartTransaction = "StartTransaction";
                public const string UpdateTransaction = "UpdateTransaction";
                public const string FinishTransaction = "FinishTransaction";
                public const string GetStartedTransactionList = "GetStartedTransactionList";
                public const string GetLastTransactionResponse = "GetLastTransactionResponse";
            }

            public static class Export
            {
                public const string ArchiveExport = "ArchiveExport";
                public const string ExportFilteredByTransactionNumber = "ExportFilteredByTransactionNumber";
                public const string ExportFilteredByTransactionNumberInterval = "ExportFilteredByTransactionNumberInterval";
                public const string ExportFilteredByPeriodOfTime = "ExportFilteredByPeriodOfTime";
                public const string GetExportData = "GetExportData";
                public const string FinalizeExport = "FinalizeExport";
                public const string CancelExport = "CancelExport";
                public const string GetLogMessageCertificate = "GetLogMessageCertificate";
            }

            public static class Information
            {
                public const string GetStorageInfo = "GetStorageInfo";
            }
        }
    }
}
