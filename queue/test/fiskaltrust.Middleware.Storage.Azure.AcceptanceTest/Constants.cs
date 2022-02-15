using System;

namespace fiskaltrust.Middleware.Storage.Azure.AcceptanceTest
{
    public static class Constants
    {
        public static readonly string AzureStorageConnectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING_AZURE_STORAGE_TESTS");
    }
}
