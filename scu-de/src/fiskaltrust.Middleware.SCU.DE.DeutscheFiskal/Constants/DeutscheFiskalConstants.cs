using fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Helpers;

namespace fiskaltrust.Middleware.SCU.DE.DeutscheFiskal.Constants
{
    public static class DeutscheFiskalConstants
    {
        public const int DefaultPort = 20001;

        public static class TransactionType
        {
            public const string StartTransaction = "StartTransaction";
        }

        public static class Paths
        {
            public static string EmbeddedJava => EnvironmentHelpers.IsWindows ? "bin/jre/bin/java.exe" : "bin/jre/bin/java";
            public const string FccServiceJar = "lib/fiskal-cloud-connector-service.jar";
            public const string FccDeployJar = "lib/fcc-deploy-archive.jar";
            public const string RunFccScriptWindows = "run_fcc.bat";
            public const string RunFccScriptLinux = "run_fcc.sh";
        }

        public static class ErrorTypes
        {
            public const string InvalidRegistrationKey = "ERS_INVALID_REGISTRATION_KEY";
        }
    }
}
