using System.Collections.Generic;
using System.IO;
using System.Linq;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers.Tar;

namespace fiskaltrust.Middleware.SCU.DE.Helpers.TLVLogParser.Tar
{
    public static class TarFileHelper
    {
        public static void FinalizeTarFile(string targetFile)
        {
            using var targetStream = File.OpenWrite(targetFile);
            targetStream.Position = targetStream.Length;
            using var tarWriter = new TarWriter(targetStream, new TarWriterOptions(CompressionType.None, finalizeArchiveOnClose: true));
        }

        public static void AppendTarStreamToTarFile(string targetFile, Stream inputTarStream)
        {
            using var reader = ReaderFactory.Open(inputTarStream);

            var existingEntries = File.Exists(targetFile) ? GetNonSignatureEntriesFromTarFile(targetFile) : new List<string>();

            using var targetStream = File.OpenWrite(targetFile);
            targetStream.Position = targetStream.Length;
            using var tarWriter = new TarWriter(targetStream, new TarWriterOptions(CompressionType.None, finalizeArchiveOnClose: false) { LeaveStreamOpen = false });

            while (reader.MoveToNextEntry())
            {
                if (!existingEntries.Any(ee => ee == reader.Entry.Key))
                {
                    using var entryStream = reader.OpenEntryStream();
                    tarWriter.Write(reader.Entry.Key, entryStream, null, reader.Entry.Size);
                }
            }
        }

        private static List<string> GetNonSignatureEntriesFromTarFile(string targetFile)
        {
            using var tarArchive = TarArchive.Open(targetFile, new ReaderOptions { LeaveStreamOpen = false });
            return tarArchive.Entries.Where(x => x.Key.EndsWith(".csv") || x.Key.EndsWith(".crt") || x.Key.EndsWith(".pem") || x.Key.EndsWith(".cer")).Select(x => x.Key).ToList();
        }

        public static string GetLastLogEntryFromTarFile(string targetFile)
        {
            using var tarArchive = TarArchive.Open(targetFile, new ReaderOptions { LeaveStreamOpen = false });
            return tarArchive.Entries.Last(x => x.Key.EndsWith(".log")).Key;
        }
    }
}
