using System;
using Microsoft.Azure.Cosmos.Table;

namespace fiskaltrust.Middleware.Storage.Azure.Repositories
{
    public abstract class BaseAzureRepository
    {
        protected CloudStorageAccount CloudStorageAccount { get; }

        protected BaseAzureRepository(string connStr)
        {
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new ArgumentNullException(nameof(connStr));
            }

            CloudStorageAccount = CloudStorageAccount.Parse(connStr);
        }
    }
}
