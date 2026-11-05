using System;
using System.IO;
using SharpCompress.Readers;

namespace fiskaltrust.Middleware.Journal.DE.TAR.Helpers
{
    public static class ZipHelper
    {
        public static void ExtractBase64ZipContentToDisk(string zipContentBase64, string targetFile)
        {
            var zipBytes = Convert.FromBase64String(zipContentBase64);

            using (var stream = new MemoryStream(zipBytes))
            using (var reader = ReaderFactory.Open(stream))
            {
                reader.MoveToNextEntry();
                reader.WriteEntryTo(targetFile);
            }
        }
    }
}
