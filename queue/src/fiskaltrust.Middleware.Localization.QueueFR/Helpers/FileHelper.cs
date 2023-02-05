using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace fiskaltrust.Middleware.Localization.QueueFR.Helpers
{
    public static class FileHelper
    {
        public static IEnumerable<List<byte>> ReadFileAsChunks(string path, int maxChunkSize)
        {
            using (var inFileSteam = new FileStream(path, FileMode.Open))
            {
                var bufferSize = maxChunkSize > 0 ? maxChunkSize : 1024 * 4;
                var buffer = new byte[bufferSize];
                var bytesRead = inFileSteam.Read(buffer, 0, buffer.Length);

                while (bytesRead > 0)
                {
                    yield return buffer.Take(bytesRead).ToList();
                    buffer = new byte[bufferSize];
                    bytesRead = inFileSteam.Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
