using System;
using fiskaltrust.Middleware.Contracts.Models.Transactions;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using Azure.Data.Tables;
using Azure.Core;
using Azure;
using System.Linq;
using System.Text;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.Configuration;
using fiskaltrust.Middleware.Storage.AzureTableStorage.TableEntities.MasterData;
using fiskaltrust.Middleware.Storage.AzureTableStorage.Repositories;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping
{
    public static class Mapper
    {
        // 64KiB, roughly 32K characters
        public const int MAX_STRING_CHARS = 32_000;

        public static string GetHashString(long value)
        {
            var descRowBytes = BitConverter.GetBytes(long.MaxValue - value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(descRowBytes);
            }
            return BitConverter.ToString(descRowBytes);
        }
    }
}
