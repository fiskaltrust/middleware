using fiskaltrust.ifPOS.v1;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fiskaltrust.Middleware.Interface.Client.Extensions
{
    public static class StreamExtensions
    {
        public static async IAsyncEnumerable<JournalResponse> ToAsyncEnumerable(this Stream stream, int chunkSize)
        {
            var buffer = new byte[chunkSize];
            int readAmount;
            while ((readAmount = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                yield return new JournalResponse
                {
                    Chunk = buffer.Take(readAmount).ToList()
                };
                buffer = new byte[chunkSize];
            }
            stream.Dispose();
            yield break;
        }
    }
}
