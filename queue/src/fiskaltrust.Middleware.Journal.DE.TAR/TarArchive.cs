using System;
using System.Collections.Generic;
using System.IO;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers.Tar;

namespace fiskaltrust.Middleware.Journal.DE.TAR
{
    public sealed class TarArchive : IDisposable
    {
        private readonly HashSet<string> _alreadyAddedFiles;
        private readonly string _targetFile;

        public TarArchive(string targetFile)
        {
            _alreadyAddedFiles = new HashSet<string>();
            _targetFile = targetFile;
        }

        public void AppendExistingTarFile(string sourceFileToAppend)
        {
            using var inputTarStream = File.OpenRead(sourceFileToAppend);
            using var reader = ReaderFactory.Open(inputTarStream);

            using var targetStream = File.OpenWrite(_targetFile);
            targetStream.Position = targetStream.Length;
            using var tarWriter = new TarWriter(targetStream, new TarWriterOptions(CompressionType.None, finalizeArchiveOnClose: false) { LeaveStreamOpen = false });

            while (reader.MoveToNextEntry())
            {
                if (!_alreadyAddedFiles.Contains(reader.Entry.Key))
                {
                    using var entryStream = reader.OpenEntryStream();
                    tarWriter.Write(reader.Entry.Key, entryStream, null, reader.Entry.Size);
                    _alreadyAddedFiles.Add(reader.Entry.Key);
                }
            }
        }

        public void Dispose()
        {
            if (File.Exists(_targetFile))
            {
                FinalizeTarFile();
            }
        }

        private void FinalizeTarFile()
        {
            using var targetStream = File.OpenWrite(_targetFile);
            targetStream.Position = targetStream.Length;
            using var tarWriter = new TarWriter(targetStream, new TarWriterOptions(CompressionType.None, finalizeArchiveOnClose: true));
        }
    }
}
