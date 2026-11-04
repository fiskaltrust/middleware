using Moq;
using Xunit;
using Microsoft.Extensions.Logging;
using fiskaltrust.storage.V0;
using System.IO;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text;
using SharpCompress.Common;
using SharpCompress.Writers.Tar;
using SharpCompress.Writers.Zip;

namespace fiskaltrust.Middleware.Journal.DE.TAR.UnitTest
{
    public class TarExporterUnitTests
    {
        [Fact]
        public async Task ExportAsync_Should_Pack_Every_Journal()
        {
            var logMock = new Mock<ILogger>();
            var journalRepo = new Mock<IReadOnlyJournalDERepository>(MockBehavior.Strict);

            journalRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<ftJournalDE>
            {
                new ftJournalDE { ftJournalDEId = Guid.NewGuid(), FileName = "a", FileContentBase64 = "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" },
                new ftJournalDE { ftJournalDEId = Guid.NewGuid(), FileName = "b", FileContentBase64 = "UEsDBBQAAAAIAO17mlTEIUcHiQUAAAAYAAAnAAAAMjAyMjA0MjYwMzMxMjUxMzdfdGVzdC1zd2l0Y2gtMTIzNDUudGFy7ZcJTBRXGMdndgdWEGUpikeLLFjCOfje3GMlBVHAKrbIUSHG7ezszLIIu7q7oFKbAgoaL0TiEY3VgmjTRqum0IoHVVtDvWI5bJumnmljLbXUSNXYxD6wVqStNFYhGv7Jy5vvZeZ7733f+30zY7Wp9ijZmY89QQEkjmE6e6TuPaBoHoMMRUOepwCL7oOAp2jMAJ7kou4pz+mSHGgp/9dP9809JQoyK07ZYZ3tstptY4Mig1Src5aUM9/gtFpsBjnHnmcmU1MmGkJFkyKKQDaTZpkTSEaGKinKsonkZGhSzKrKs6o5DD2fK9nyVEl25TkUR1d/CbmmRGTmKw7nw2fKh1EgiiVhFB0Fgjz7OjzPvNJs1nkuI+RYIPKiINDGFKuFZCjjFLuFTHVIxql2kjWmIERcxrgcq2JzkS7F6SKdc60uOYuEFM2wUTl2y0Pn6JF/Hnbhn8ZQQQAU089/bwgU1WpwjbsHgb2N6bQ6HC/Ud2Yb5d7mRBwjWIt8/pbyYmwhbijB3QjD+5tvJq66MbL8fF5ypj49fOVn+vrJt164XLHLr6QErp0CgZe7Z6dvHCdwrQYP1xAmy/UIIiaUaG6OrCpIWRqq9Y2/nXK9obxWe3JP6sERbef88Eh654XX0guPr9ev2BFgDMxJcoWUTqg8np1mtVmrL4dObQua09eBe0b0z/zTD/Ifj8LuzHrkAtAT/zQNuvMPmX7+e0WgqKUb/z53s91DAfDOtdtcWTnzSfTmdlptloX6FLvN6bJaFEe63WGRbJZHKg8R98rDbwvYpdtm1yS3LTvhtuTNNe3Fu47XBZZTmHmSJSxkubu491pC4U/hlR63D0b6b7t5cvOxdbWXLzbMDliQtxgnY4Xn+jquT4skSVRVgRFEoPJQEqECeJaFrAQBy1GSoMgMBCaVMakilBVBpE0AcgIrsDQUOUamoXE6qhxRZsXxkDl64p+nuAe//ymIBvv57w2BYrwRtYwtCD6NJhDrEVhP9wHhpYmnogmtFkA4FPi6a9MIT1+vrh/6cDjw6xjW+nrfG05FFcQQFwtGDRtEUYCBFDoRqGUik+5iotT/i8sRYNhdl/oHXKJfBpCBntB1LkqD/7k6La7TjseIHWNfCfw+dfnRcN3A4RfL1gZmLqOnxg90O+B7vaqe/GrChfJ2n9yGqwXBAfttjm/jyiLuvDVJ4tJ/PL3l0upU/MLikqpY8DIYjOb198bxOwSh1egKUclCtm+HrUHpDuiwRhNox4VD1mbEHjPjde9Uu959b8Uaq9pS+kNz14BpLRgwa8CEwk37Wl//PCG6dc4sr2tU9OEZZ+C6gJqYQ03BhqKPdV+ExY3PSLadebV+fv0bpXP97BrglR72wUt14ypaYraObnpefrF0StL67eYro060gIsN1ecaI6MLV0064pXtzTd7GWPkef89/2ZZogTKxLCCwMoqx5gpkwAkUVAYjpNoQCscL4oqz5ugKrEizzMmljFxCroQaYmX5MfDP9Odf4pj+/nvDSH2T6OWfpd/PPxx8A0BTTFQYCiWzezE/b75KC7zgf4+3m4RRQQWpDVhhNh4p3XcYUfIjH1Xph2YufVUvKYgiVCvZiaO2JvgNvLDsuE5Ufrl2Qf2//zJaqly7qqBBOazva3aPmaIrsI04EjGoeRBG3dOGzIWFriSPo34vUWs8aeOJlU1LfIY0xj/TXHZR1XjQcyD7GsWAe+/2HcDWtQB/46BwYQf0TP8WRhQNCAslAzxrdQfGzOzfdPmr7/MXnJ+5Z6JzVzaQmay355fVmyI3n22+PZgz1CP1oztBeuCfTQQa48fWnUm4dbZX2sCPesqNiVfihDn3YgM/c6vaZ+ePxidsjtBZ585vQGWL/WvDd6wUdvXZ6pf/erX06E/AFBLAQIUABQAAAAIAO17mlTEIUcHiQUAAAAYAAAnAAAAAAAAAAAAAAAAAAAAAAAyMDIyMDQyNjAzMzEyNTEzN190ZXN0LXN3aXRjaC0xMjM0NS50YXJQSwUGAAAAAAEAAQBVAAAAzgUAAAAA"}
            });

            var targetFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".tar";

            var sut = new TarExporter(logMock.Object, journalRepo.Object);
            var result = await sut.ExportAsync(targetFile);

            result.JournalFilesRead.Should().Be(2);
            result.JournalFilesPacked.Should().Be(2);
            result.IsComplete.Should().BeTrue();

            File.Exists(targetFile).Should().BeTrue();
            new FileInfo(targetFile).Length.Should().Be(9216);
            File.Delete(targetFile);
        }

        [Fact]
        public async Task ExportAsync_Should_Report_Every_Journal_That_Could_Not_Be_Packed()
        {
            var logMock = new Mock<ILogger>();
            var journalRepo = new Mock<IReadOnlyJournalDERepository>(MockBehavior.Strict);
            var corruptJournalId = Guid.NewGuid();

            journalRepo.Setup(x => x.GetAsync()).ReturnsAsync(new List<ftJournalDE>
            {
                new ftJournalDE { ftJournalDEId = Guid.NewGuid(), FileName = "a", FileContentBase64 = CreateZippedTar("a.log") },
                new ftJournalDE { ftJournalDEId = corruptJournalId, FileName = "b", FileContentBase64 = "this is not a base64 zip" },
                new ftJournalDE { ftJournalDEId = Guid.NewGuid(), FileName = "c", FileContentBase64 = CreateZippedTar("c.log") }
            });

            var targetFile = Path.GetTempPath() + Guid.NewGuid().ToString() + ".tar";

            var sut = new TarExporter(logMock.Object, journalRepo.Object);
            var result = await sut.ExportAsync(targetFile);

            result.JournalFilesRead.Should().Be(3);
            result.JournalFilesPacked.Should().Be(2);
            result.IsComplete.Should().BeFalse();
            var failure = result.Failures.Should().ContainSingle().Subject;
            failure.JournalDEId.Should().Be(corruptJournalId);
            failure.FileName.Should().Be("b");
            failure.Exception.Should().NotBeNull();

            // The journals that could be packed are still in the archive; the caller decides whether it may be delivered.
            File.Exists(targetFile).Should().BeTrue();
            File.Delete(targetFile);
        }

        // A JournalDE's content is a ZIP holding one TAR file; the exporter appends that TAR's entries to the export.
        private static string CreateZippedTar(string entryName)
        {
            using var tarStream = new MemoryStream();
            using (var tarWriter = new TarWriter(tarStream, new TarWriterOptions(CompressionType.None, finalizeArchiveOnClose: true) { LeaveStreamOpen = true }))
            {
                var content = Encoding.UTF8.GetBytes($"content of {entryName}");
                using var entryStream = new MemoryStream(content);
                tarWriter.Write(entryName, entryStream, null, content.Length);
            }

            using var zipStream = new MemoryStream();
            using (var zipWriter = new ZipWriter(zipStream, new ZipWriterOptions(CompressionType.Deflate) { LeaveStreamOpen = true }))
            {
                tarStream.Position = 0;
                zipWriter.Write("journal.tar", tarStream, DateTime.UtcNow);
            }

            return Convert.ToBase64String(zipStream.ToArray());
        }
    }
}
