using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    public static class Constants
    {
        public static readonly string AzureStorageConnectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING_AZURE_STORAGE_TESTS");
    }
}
