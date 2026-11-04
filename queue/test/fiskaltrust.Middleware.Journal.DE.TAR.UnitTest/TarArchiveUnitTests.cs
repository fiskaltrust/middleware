using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.V0;
using System.IO;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using fiskaltrust.Middleware.Journal.DE.TAR.Models;
using System;
using SharpCompress.Writers.Tar;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.Linq;

namespace fiskaltrust.Middleware.Journal.DE.TAR.UnitTest
{
    public class TarArchiveUnitTests
    {
        [Fact]
        public void AppendExistingTarFile_DoesNot_AddDuplicates()
        {
            var sourceFile1 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".tar";
            var sourceFile2 = Path.GetTempPath() + Guid.NewGuid().ToString() + ".tar";

            CreateTarFile(sourceFile1, new()
            {
                ("test1.txt", new byte[1]),
                ("test2.txt", new byte[1]),
                ("test3.txt", new byte[1]),
            });
            CreateTarFile(sourceFile2, new()
            {
                ("test2.txt", new byte[1]),
                ("test3.txt", new byte[1]),
                ("test4.txt", new byte[1]),
            });

            var journalRepo = new Mock<IReadOnlyJournalDERepository>(MockBehavior.Strict);
            journalRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<ftJournalDE>
            {
                new ftJournalDE{ftJournalDEId = Guid.NewGuid(), FileName = Path.GetFileName(sourceFile1), FileContentBase64 = Convert.ToBase64String(File.ReadAllBytes(sourceFile1))},
                new ftJournalDE{ftJournalDEId = Guid.NewGuid(), FileName = Path.GetFileName(sourceFile2), FileContentBase64 = Convert.ToBase64String(File.ReadAllBytes(sourceFile2))}
            });

            var targetFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".tar";

            using (var sut = new TarArchive(targetFile))
            {
                sut.AppendExistingTarFile(sourceFile1);
                sut.AppendExistingTarFile(sourceFile2);
            }

            var tarFileContent = GetTarFileContent(targetFile).ToList();
            tarFileContent.Should().HaveCount(4);
            tarFileContent.Should().BeEquivalentTo(new List<string> { "test1.txt", "test2.txt", "test3.txt", "test4.txt" });
        }

        private void CreateTarFile(string targetFile, List<(string fileName, byte[] content)> content)
        {
            using var targetStream = File.OpenWrite(targetFile);
            using var tarWriter = new TarWriter(targetStream, new TarWriterOptions(CompressionType.None, finalizeArchiveOnClose: false) { LeaveStreamOpen = false });

            foreach (var item in content)
            {
                using var entryStream = new MemoryStream(item.content);
                tarWriter.Write(item.fileName, entryStream, null, item.content.Length);
            }
        }

        private IEnumerable<string> GetTarFileContent(string tarFile)
        {
            using var inputTarStream = File.OpenRead(tarFile);
            using var reader = ReaderFactory.Open(inputTarStream);

            while (reader.MoveToNextEntry())
            {
                yield return reader.Entry.Key;
            }
        }
    }
}
