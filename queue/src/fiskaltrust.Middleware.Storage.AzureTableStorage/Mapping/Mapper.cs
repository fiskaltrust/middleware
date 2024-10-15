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
        private const int MAX_STRING_CHARS = 32_000;

        private const string OVERSIZED_MARKER = "oversize";

        public static void SetOversized(this TableEntity entity, string property, string value)
        {
            var currentRequestChunk = 0;
            var chunks = value.Chunk(MAX_STRING_CHARS);

            if (chunks.Count() == 1)
            {
                entity.Add($"{property}", chunks.First());
            }
            else
            {
                foreach (var chunk in chunks)
                {
                    entity.Add($"{property}_{OVERSIZED_MARKER}_{currentRequestChunk}", chunk);
                    currentRequestChunk++;
                }
            }
        }

        public static string GetOversized(this TableEntity entity, string property)
        {
            var request = entity.GetString(property);
            if (!string.IsNullOrEmpty(request))
            {
                return request;
            }
            else
            {
                var reqSb = new StringBuilder();
                foreach (var key in entity.Keys.Where(x => x.StartsWith($"{property}_{OVERSIZED_MARKER}_")))
                {
                    reqSb.Append(entity[key]);
                }
                return reqSb.ToString();
            }
        }

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
