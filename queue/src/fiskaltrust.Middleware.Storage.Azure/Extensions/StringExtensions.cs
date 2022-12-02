using System;
using System.Collections.Generic;

namespace fiskaltrust.Middleware.Storage.Azure.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<string> Chunk(this string str, int maxChunkSize)
        {
            if (string.IsNullOrEmpty(str))
            {
                yield break;
            }

            for (var i = 0; i < str.Length; i += maxChunkSize)
            {
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
            }
        }
    }
}
