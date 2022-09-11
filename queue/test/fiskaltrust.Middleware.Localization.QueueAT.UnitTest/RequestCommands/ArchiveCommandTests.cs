using System;
using System.CodeDom;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.Middleware.Contracts.Models;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueFR;
using fiskaltrust.Middleware.Localization.QueueFR.Constants;
using fiskaltrust.Middleware.Localization.QueueFR.Factories;
using fiskaltrust.Middleware.Localization.QueueFR.Models;
using fiskaltrust.Middleware.Localization.QueueFR.RequestCommands;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueAT.UnitTest.RequestCommands
{
    public class ArchiveCommandTests
    {
        [Fact]
        public void Validate_ShouldReturnErrors_WhenChargeItemsAreSent()
        {
            var sut = GetSut();
            var request = new ReceiptRequest
            {
                cbChargeItems = new[] { new ChargeItem() }
            };
            sut.Validate(null, null, request, null).Should().HaveCount(1);
        }

        [Fact]
        public void Validate_ShouldReturnErrors_WhenPayItemsAreSent()
        {
            var sut = GetSut();
            var request = new ReceiptRequest
            {
                cbPayItems = new[] { new PayItem() }
            };
            sut.Validate(null, null, request, null).Should().HaveCount(1);
        }

        [Fact]
        public void Validate_ShouldReturnNoErrors_WhenNoChargeOrPayItemsAreSent()
        {
            var sut = GetSut();
            var request = new ReceiptRequest
            {
                cbPayItems = new PayItem[0],
                cbChargeItems = new ChargeItem[0]
            };
            sut.Validate(null, null, request, null).Should().BeEmpty();
        }

        [Fact]
        public async Task Execute_ShouldCreateTrainingReceipt_WhenTrainingFlagIsSet()
        {
            var queue = new ftQueue { ftReceiptNumerator = 100, ftQueueId = Guid.NewGuid() };
            var queueFr = new ftQueueFR { CashBoxIdentification = "cb-1" };
            var queueItem = new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueRow = 120 };
            var scu = new ftSignaturCreationUnitFR
            {
                Siret = "123",
                CertificateSerialNumber = "123-123"
            };
            var request = new ReceiptRequest
            {
                ftQueueID = queue.ftQueueId.ToString(),
                ftReceiptCase = ((long) ReceiptCaseFR.ArchiveReceipt) | 0x0000_0000_0002_0000,
                cbTerminalID = "T1",
                cbReceiptReference = "R100",
                ftCashBoxID = Guid.NewGuid().ToString()
            };

            var signatureFactoryMock = new Mock<ISignatureFactoryFR>();
            signatureFactoryMock.Setup(x => x.CreateTotalsSignature(It.IsAny<ReceiptResponse>(), It.IsAny<ftQueue>(), It.IsAny<ftSignaturCreationUnitFR>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns(("testhash", new SignaturItem(), new ftJournalFR()));
            signatureFactoryMock.Setup(x => x.CreateTotalsSignatureWithoutSigning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns<string, string, SignaturItem.Formats, SignaturItem.Types>((_, description, _, _) => new SignaturItem { Caption = description });

            var sut = GetSut(signatureFactoryMock.Object);

            var (receiptResponse, journalFR, actionJournals) = await sut.ExecuteAsync(queue, queueFr, scu, request, queueItem);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "mode école");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Day Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Archive Totals");
            receiptResponse.ftReceiptFooter.Should().Contain("T R A I N I N G");

            journalFR.ReceiptType.Should().Be("X");

            queueFr.XTotalizer.Should().Be(0);
            queueFr.XLastHash.Should().Be("testhash");
            queueFr.XNumerator.Should().Be(1);

            queueFr.ATotalizer.Should().Be(0);
            queueFr.ANumerator.Should().Be(0);
            queueFr.ALastHash.Should().BeNull();
        }

        [Fact]
        public async Task Execute_ShouldCreateSignedArchiveReceipt_ForFirstArchiveReceipt()
        {
            var queue = new ftQueue { ftReceiptNumerator = 100, ftQueueId = Guid.NewGuid() };
            var queueFr = new ftQueueFR { CashBoxIdentification = "cb-1" };
            var queueItem = new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueRow = 120 };
            var scu = new ftSignaturCreationUnitFR
            {
                Siret = "123",
                CertificateSerialNumber = "123-123"
            };
            var request = new ReceiptRequest
            {
                ftQueueID = queue.ftQueueId.ToString(),
                ftReceiptCase = (long) ReceiptCaseFR.ArchiveReceipt,
                cbTerminalID = "T1",
                cbReceiptReference = "R100",
                ftCashBoxID = Guid.NewGuid().ToString()
            };

            var signatureFactoryMock = new Mock<ISignatureFactoryFR>();
            signatureFactoryMock.Setup(x => x.CreateTotalsSignature(It.IsAny<ReceiptResponse>(), It.IsAny<ftQueue>(), It.IsAny<ftSignaturCreationUnitFR>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns(("testhash", new SignaturItem(), new ftJournalFR()));
            signatureFactoryMock.Setup(x => x.CreateTotalsSignatureWithoutSigning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns<string, string, SignaturItem.Formats, SignaturItem.Types>((_, description, _, _) => new SignaturItem { Caption = description });

            var receiptJournalMock = new Mock<IMiddlewareReceiptJournalRepository>();
            receiptJournalMock.Setup(x => x.GetWithLastTimestampAsync()).ReturnsAsync(new ftReceiptJournal());
            receiptJournalMock.Setup(x => x.GetByReceiptNumber(1)).ReturnsAsync(new ftReceiptJournal());

            var queueItemRepoMock = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepoMock.Setup(x => x.GetAsync(Guid.Empty)).ReturnsAsync(new ftQueueItem());

            var sut = GetSut(signatureFactoryMock.Object, receiptJournalMock.Object, queueItemRepoMock.Object);

            var (receiptResponse, journalFR, actionJournals) = await sut.ExecuteAsync(queue, queueFr, scu, request, queueItem);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Day Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Archive Totals");

            journalFR.ReceiptType.Should().Be("A");

            queueFr.XTotalizer.Should().Be(0);
            queueFr.XLastHash.Should().BeNull();

            queueFr.ANumerator.Should().Be(1);
            queueFr.ALastHash.Should().Be("testhash");
        }

        [Fact]
        public async Task Execute_ShouldCreateSignedArchiveReceipt_ForNonFirstArchiveReceipt()
        {
            var lastQueueItemId = Guid.NewGuid();
            var queue = new ftQueue { ftReceiptNumerator = 100, ftQueueId = Guid.NewGuid() };
            var queueFr = new ftQueueFR { CashBoxIdentification = "cb-1", ALastQueueItemId = lastQueueItemId, ANumerator = 1 };
            var queueItem = new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueRow = 120 };
            var scu = new ftSignaturCreationUnitFR
            {
                Siret = "123",
                CertificateSerialNumber = "123-123"
            };
            var request = new ReceiptRequest
            {
                ftQueueID = queue.ftQueueId.ToString(),
                ftReceiptCase = (long) ReceiptCaseFR.ArchiveReceipt,
                cbTerminalID = "T1",
                cbReceiptReference = "R100",
                ftCashBoxID = Guid.NewGuid().ToString()
            };

            var signatureFactoryMock = new Mock<ISignatureFactoryFR>();
            signatureFactoryMock.Setup(x => x.CreateTotalsSignature(It.IsAny<ReceiptResponse>(), It.IsAny<ftQueue>(), It.IsAny<ftSignaturCreationUnitFR>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns(("testhash", new SignaturItem(), new ftJournalFR()));
            signatureFactoryMock.Setup(x => x.CreateTotalsSignatureWithoutSigning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns<string, string, SignaturItem.Formats, SignaturItem.Types>((_, description, _, _) => new SignaturItem { Caption = description });

            var receiptJournalMock = new Mock<IMiddlewareReceiptJournalRepository>();
            receiptJournalMock.Setup(x => x.GetWithLastTimestampAsync()).ReturnsAsync(new ftReceiptJournal());

            var queueItemRepoMock = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepoMock.Setup(x => x.GetAsync(Guid.Empty)).ReturnsAsync(new ftQueueItem());
            queueItemRepoMock.Setup(x => x.GetAsync(lastQueueItemId)).ReturnsAsync(new ftQueueItem
            {
                response = JsonConvert.SerializeObject(new ReceiptResponse())
            });

            var sut = GetSut(signatureFactoryMock.Object, receiptJournalMock.Object, queueItemRepoMock.Object);

            var (receiptResponse, journalFR, actionJournals) = await sut.ExecuteAsync(queue, queueFr, scu, request, queueItem);
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Day Totals");
            receiptResponse.ftSignatures.Should().Contain(x => x.Caption == "Archive Totals");

            journalFR.ReceiptType.Should().Be("A");

            queueFr.XTotalizer.Should().Be(0);
            queueFr.XLastHash.Should().BeNull();

            queueFr.ANumerator.Should().Be(2);
            queueFr.ALastHash.Should().Be("testhash");
        }

        [Fact]
        public async Task Execute_ShouldCreateCreateExport_WhenExportIsEnabled()
        {
            var lastQueueItemId = Guid.NewGuid();
            var queue = new ftQueue { ftReceiptNumerator = 100, ftQueueId = Guid.NewGuid() };
            var queueFr = new ftQueueFR { CashBoxIdentification = "cb-1", ALastQueueItemId = lastQueueItemId, ANumerator = 1 };
            var queueItem = new ftQueueItem { ftQueueItemId = Guid.NewGuid(), ftQueueRow = 120 };
            var scu = new ftSignaturCreationUnitFR
            {
                Siret = "123",
                CertificateSerialNumber = "123-123"
            };
            var request = new ReceiptRequest
            {
                ftQueueID = queue.ftQueueId.ToString(),
                ftReceiptCase = (long) ReceiptCaseFR.ArchiveReceipt,
                cbTerminalID = "T1",
                cbReceiptReference = "R100",
                ftCashBoxID = Guid.NewGuid().ToString()
            };

            var signatureFactoryMock = new Mock<ISignatureFactoryFR>();
            signatureFactoryMock.Setup(x => x.CreateTotalsSignature(It.IsAny<ReceiptResponse>(), It.IsAny<ftQueue>(), It.IsAny<ftSignaturCreationUnitFR>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns(("testhash", new SignaturItem(), new ftJournalFR()));
            signatureFactoryMock.Setup(x => x.CreateTotalsSignatureWithoutSigning(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SignaturItem.Formats>(), It.IsAny<SignaturItem.Types>()))
                .Returns<string, string, SignaturItem.Formats, SignaturItem.Types>((_, description, _, _) => new SignaturItem { Caption = description });

            var receiptJournalMock = new Mock<IMiddlewareReceiptJournalRepository>();
            receiptJournalMock.Setup(x => x.GetWithLastTimestampAsync()).ReturnsAsync(new ftReceiptJournal());

            var queueItemRepoMock = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepoMock.Setup(x => x.GetAsync(Guid.Empty)).ReturnsAsync(new ftQueueItem());
            queueItemRepoMock.Setup(x => x.GetAsync(lastQueueItemId)).ReturnsAsync(new ftQueueItem
            {
                response = JsonConvert.SerializeObject(new ReceiptResponse())
            });

            var middlewareConfig = new MiddlewareConfiguration
            {
                Configuration = new() { { "frarchiveexport", true } },
                ServiceFolder = Path.GetTempPath()
            };

            var archiveProcessorMock = new Mock<IArchiveProcessor>();

            var sut = GetSut(signatureFactoryMock.Object, receiptJournalMock.Object, queueItemRepoMock.Object, middlewareConfig, archiveProcessorMock.Object);
            
            var (receiptResponse, journalFR, actionJournals) = await sut.ExecuteAsync(queue, queueFr, scu, request, queueItem);

            archiveProcessorMock.Verify(x => x.ExportArchiveDataAsync(It.IsAny<string>(), It.IsAny<ArchivePayload>(), It.IsAny<ftSignaturCreationUnitFR>()), Times.Once);
        }

        private ArchiveCommand GetSut(ISignatureFactoryFR signatureFactoryFR = null, IMiddlewareReceiptJournalRepository receiptJournalRepository = null,
            IMiddlewareQueueItemRepository queueItemRepository = null, MiddlewareConfiguration config = null, IArchiveProcessor archiveProcessor = null)
        {
            var middlewareConfig = config ?? new MiddlewareConfiguration
            {
                Configuration = new()
            };

            var signatureFactoryMock = signatureFactoryFR ?? Mock.Of<ISignatureFactoryFR>();
            var archiveProcesorMock = archiveProcessor ?? Mock.Of<IArchiveProcessor>();
            var actionJournalRepoMock = Mock.Of<IMiddlewareActionJournalRepository>();
            var queueItemRepoMock = queueItemRepository ?? Mock.Of<IMiddlewareQueueItemRepository>();
            var receiptJournalRepoMock = receiptJournalRepository ?? Mock.Of<IMiddlewareReceiptJournalRepository>();
            var journalFRRepoMock = Mock.Of<IMiddlewareJournalFRRepository>();
            var loggerMock = Mock.Of<ILogger<ArchiveCommand>>();

            return new ArchiveCommand(signatureFactoryMock, middlewareConfig, archiveProcesorMock,
                actionJournalRepoMock, queueItemRepoMock, receiptJournalRepoMock, journalFRRepoMock, loggerMock);
        }
    }
}
