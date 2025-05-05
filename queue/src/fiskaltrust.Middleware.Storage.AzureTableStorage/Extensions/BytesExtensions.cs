using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Storage.AzureTableStorage.Extensions
{
    public static class BytesExtensions
    {
        public static IEnumerable<ArraySegment<byte>> Chunk(this byte[] source, int chunkSize)
        {
            for (var i = 0; i < source.Length; i += chunkSize)
            {
                var chunk = new ArraySegment<byte>(source, i, Math.Min(chunkSize, source.Length - i));
                yield return chunk;
            }
        }
    }
}
