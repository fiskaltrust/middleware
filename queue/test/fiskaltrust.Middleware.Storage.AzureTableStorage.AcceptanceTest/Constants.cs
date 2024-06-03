using System;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.AcceptanceTest
{
    public static class Constants
    {
        public static readonly string AzureStorageConnectionString = Environment.GetEnvironmentVariable("CONNECTIONSTRING_AZURE_STORAGE_TESTS") ?? "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;QueueEndpoint=http://localhost:10001/devstoreaccount1;TableEndpoint=http://localhost:10002/devstoreaccount1;";
    }
}
