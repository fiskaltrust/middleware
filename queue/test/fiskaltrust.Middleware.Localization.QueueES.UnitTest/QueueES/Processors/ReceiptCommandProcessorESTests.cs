using System;
using System.IO;
using System.Threading.Tasks;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueueES.ESSSCD;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.Middleware.Storage.ES;
using fiskaltrust.storage.V0;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;
using fiskaltrust.Middleware.Localization.v2.Models.ifPOS.v2.Cases;
using fiskaltrust.storage.V0.MasterData;
using AutoFixture;
using fiskaltrust.Middleware.Localization.v2.Storage;
using System.Text.Json;
using fiskaltrust.Middleware.Contracts.Factories;
using System.Text;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Contracts.Repositories;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.QueueES.Processors
{
    public class ReceiptCommandProcessorESTests
    {
        private readonly Fixture _fixture;

        public ReceiptCommandProcessorESTests()
        {
            _fixture = new Fixture();
            _fixture.Customize<ReceiptResponse>(c => c
                .With(r => r.ftReceiptIdentification, () => $"{_fixture.Create<uint>():X}#{_fixture.Create<string>()}")
                .With(r => r.ftSignatures, () =>
                    _fixture
                        .CreateMany<SignatureItem>()
                        .Append(Factories.SignaturItemFactory.CreateESHuella(Convert.ToBase64String(_fixture.CreateMany<byte>().ToArray())))
                        .Append(new SignatureItem
                        {
                            ftSignatureType = (long) SignatureTypesES.NIF,
                            ftSignatureFormat = (int) ifPOS.v1.SignaturItem.Formats.QR_Code,
                            Caption = "IDEmisorFactura",
                            Data = _fixture.Create<string>()
                        })
                        .ToList()
                )
            );
            _fixture.Customize<ftQueueItem>(c => c
                .With(q => q.request, () => JsonSerializer.Serialize(_fixture.Create<ReceiptRequest>()))
                .With(q => q.response, () => JsonSerializer.Serialize(_fixture.Create<ReceiptResponse>()))
            );
        }

        private readonly ReceiptCommandProcessorES _sut = new ReceiptCommandProcessorES(Mock.Of<IESSSCD>(), Mock.Of<Storage.ES.IConfigurationRepository>(), Mock.Of<IQueueItemRepository>());

        [Theory]
        [InlineData(ReceiptCases.PaymentTransfer0x0002)]
        [InlineData(ReceiptCases.PointOfSaleReceiptWithoutObligation0x0003)]
        [InlineData(ReceiptCases.ECommerce0x0004)]
        [InlineData(ReceiptCases.Protocol0x0005)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCases receiptCase)
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = (int) receiptCase
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x4553_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
            var result = await _sut.ProcessReceiptAsync(request);

            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4553_2000_0000_0000);
        }

        [Fact]
        public async Task ProcessReceiptAsync_ShouldReturnError()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = -1
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x4553_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
            var result = await _sut.ProcessReceiptAsync(request);
            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4553_2000_EEEE_EEEE);
        }

        [Fact]
        public async Task PointOfSaleReceipt0x0001Async_Should_Return_QRCodeInSignatures()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var previousQueueItem = TestHelpers.CreateQueueItem();
            var queueES = new ftQueueES()
            {
                SSCDSignQueueItemId = previousQueueItem.ftQueueItemId
            };
            var signaturCreationUnitES = new ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = _fixture.Create<MasterDataConfiguration>();
            masterDataConfiguration.Outlet.VatId = "VATTEST";

            var configMock = new Mock<storage.V0.IConfigurationRepository>();
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(It.IsAny<ftQueue>())).Returns(Task.CompletedTask);
            var configurationRepositoryMock = new Mock<Storage.ES.IConfigurationRepository>();
            configurationRepositoryMock.Setup(x => x.GetQueueESAsync(queue.ftQueueId)).ReturnsAsync(queueES);
            var queueItemRepositoryMock = new Mock<IQueueItemRepository>();
            queueItemRepositoryMock.Setup(x => x.GetAsync(previousQueueItem.ftQueueItemId)).ReturnsAsync(previousQueueItem);

            var config = new VeriFactuSCUConfiguration();
            var sut = new ReceiptCommandProcessorES(new VeriFactuSCU(signaturCreationUnitES, masterDataConfiguration, config, Mock.Of<IMiddlewareQueueItemRepository>()), configurationRepositoryMock.Object, queueItemRepositoryMock.Object);

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = 0x4553_2000_0000_0000 | (long) ReceiptCases.InitialOperationReceipt0x4001,
                cbReceiptMoment = new DateTime(2019, 12, 31),
                cbReceiptReference = "TEST",
                cbChargeItems = [
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4553_2000_0000_0008,
                        Amount = 12000.00m,
                        VATAmount = 0m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4553_2000_0000_0001,
                        Amount = 15900m,
                        VATAmount = 900m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4553_2000_0000_0006,
                        Amount = 56500m,
                        VATAmount = 6500m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = 0x4553_2000_0000_0003,
                        Amount = 98400m,
                        VATAmount = 18400m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                ]
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = 0x4553_2000_0000_0000,
                ftQueueID = queue.ftQueueId,
                ftQueueItemID = queueItem.ftQueueItemId,
                ftCashBoxIdentification = "cashBoxIdentification",

                ftQueueRow = 1,
                ftReceiptIdentification = "0#",
                ftReceiptMoment = DateTime.UtcNow,
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, receiptResponse);
            var result = await sut.PointOfSaleReceipt0x0001Async(request);

            using var scope = new AssertionScope();
            result.receiptResponse.Should().Be(receiptResponse);
            result.actionJournals.Should().BeEmpty();
            result.receiptResponse.ftSignatures.Should().NotBeEmpty();


            result.receiptResponse.ftState.Should().Be(0x4553_2000_0000_0000, because: $"ftState {result.receiptResponse.ftState:X} is different than expected.");
            var expectedSignaturItem = new SignatureItem
            {
                ftSignatureType = 0x4553_2000_0000_0001,
                ftSignatureFormat = (int) ifPOS.v1.SignaturItem.Formats.QR_Code,
                Caption = "[www.fiskaltrust.es]",
                Data = "https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=VATTEST&numserie=1%2fTEST&fecha=31-12-2019&importe=182800.00"
            };
            result.receiptResponse.ftQueueID.Should().Be(receiptResponse.ftQueueID);
            result.receiptResponse.ftQueueItemID.Should().Be(receiptResponse.ftQueueItemID);
            result.receiptResponse.ftReceiptIdentification.Should().Be("0#1/TEST");
            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);
        }
    }
}
