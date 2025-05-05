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
using System.Collections.Generic;
using System.IO;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Mapping
{
    public static class Mapper
    {
        private const int MAX_COLUMN_BYTES = 64_000;
        private const int MAX_STRING_CHARS = 32_000;

        private const string OVERSIZED_MARKER = "{0}_oversize_{1}";

        public static void SetOversized(this TableEntity entity, string property, string value)
        {
            if (value is null)
            {
                return;
            }
            if (value.Length < MAX_STRING_CHARS)
            {
                entity.Add($"{property}", value);
            }
            else
            {
                byte[] bytes;

                using (var output = new MemoryStream())
                {
                    using (var gzip = new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionMode.Compress))
                    using (var sw = new StreamWriter(gzip, Encoding.UTF8))
                    {
                        sw.Write(value);
                    }
                    bytes = output.ToArray();
                }

                var chunks = bytes.Chunk(MAX_COLUMN_BYTES);

                foreach (var (chunk, i) in chunks.Select((chunk, i) => (chunk, i)))
                {
                    entity.Add(string.Format(OVERSIZED_MARKER, property, i), chunk.Array);
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
                try
                {
                    return entity.GetOversizedBinary(property);
                }
                catch (InvalidOperationException)
                {
                    return entity.GetOversizedString(property);
                }
            }
        }

        private static string GetOversizedBinary(this TableEntity entity, string property)
        {
            using (var input = new MemoryStream())
            {
                for (var i = 0; entity.ContainsKey(string.Format(OVERSIZED_MARKER, property, i)); i++)
                {
                    entity.GetBinaryData(string.Format(OVERSIZED_MARKER, property, i)).ToStream().CopyTo(input);
                }
                using (var gzip = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
                using (var sr = new StreamReader(gzip, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static string GetOversizedString(this TableEntity entity, string property)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; entity.ContainsKey(string.Format(OVERSIZED_MARKER, property, i)); i++)
            {
                stringBuilder.Append(entity.GetString(string.Format(OVERSIZED_MARKER, property, i)));
            }
            return stringBuilder.ToString();
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
