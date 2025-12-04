using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AutoFixture;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.ifPOS.v2.es;
using fiskaltrust.Middleware.Contracts.Factories;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.QueueES.Interface;
using fiskaltrust.Middleware.Localization.QueueES.Models;
using fiskaltrust.Middleware.Localization.QueueES.Models.Cases;
using fiskaltrust.Middleware.Localization.QueueES.Processors;
using fiskaltrust.Middleware.Localization.v2;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.Middleware.Localization.v2.Storage;
using fiskaltrust.Middleware.Storage;
using fiskaltrust.storage.V0;
using fiskaltrust.storage.V0.MasterData;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueueES.UnitTest.Processors
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
                        .Append(new SignatureItem()
                        {
                            Caption = "Huella",
                            Data = Convert.ToBase64String(_fixture.CreateMany<byte>().ToArray()),
                            ftSignatureFormat = SignatureFormat.Text,
                            ftSignatureType = SignatureTypeES.Huella.As<SignatureType>()
                        })
                        .Append(new SignatureItem
                        {
                            ftSignatureType = SignatureTypeES.NIF.As<SignatureType>(),
                            ftSignatureFormat = SignatureFormat.QRCode,
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

        private readonly ReceiptProcessor _sut = new(Mock.Of<ILogger<ReceiptProcessor>>(), null!, new ReceiptCommandProcessorES(Mock.Of<ILogger<ReceiptCommandProcessorES>>(), new(() => Task.FromResult(Mock.Of<IESSSCD>())), new(() => Task.FromResult(Mock.Of<IConfigurationRepository>())), new(() => Task.FromResult(Mock.Of<IMiddlewareQueueItemRepository>())), new(() => Task.FromResult(Mock.Of<IMiddlewareJournalESRepository>()))), null!, null!, null!);


        [Theory]
        [InlineData(ReceiptCase.PaymentTransfer0x0002)]
        [InlineData(ReceiptCase.PointOfSaleReceiptWithoutObligation0x0003)]
        [InlineData(ReceiptCase.ECommerce0x0004)]
        [InlineData(ReceiptCase.DeliveryNote0x0005)]
        public async Task ProcessReceiptAsync_ShouldReturnEmptyList(ReceiptCase receiptCase)
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = receiptCase
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State)0x4553_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);

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
                ftReceiptCase = (ReceiptCase)0
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State)0x4553_2000_0000_0000,
                ftCashBoxIdentification = "cashBoxIdentification",
                ftQueueID = Guid.NewGuid(),
                ftQueueItemID = Guid.NewGuid(),
                ftQueueRow = 1,
                ftReceiptIdentification = "receiptIdentification",
                ftReceiptMoment = DateTime.UtcNow,
            };
            var result = await _sut.ProcessAsync(receiptRequest, receiptResponse, queue, queueItem);
            result.receiptResponse.Should().Be(receiptResponse);
            result.receiptResponse.ftState.Should().Be(0x4553_2000_EEEE_EEEE);
        }

        [Fact(Skip = "Make client mockable")]
        public async Task PointOfSaleReceipt0x0001Async_Should_Return_QRCodeInSignatures()
        {
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var previousQueueItem = TestHelpers.CreateQueueItem();
            var previousReceiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase)(0x4553_2000_0000_0000 | (long)ReceiptCase.InitialOperationReceipt0x4001),
                cbReceiptMoment = new DateTime(2019, 12, 31),
                cbReceiptReference = "TEST",
                cbChargeItems = [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0008,
                        Amount = 12000.00m,
                        VATAmount = 0m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0001,
                        Amount = 15900m,
                        VATAmount = 900m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0006,
                        Amount = 56500m,
                        VATAmount = 6500m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0003,
                        Amount = 98400m,
                        VATAmount = 18400m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                ]
            };
            var previousReceiptResponse = new ReceiptResponse
            {
                ftState = (State)0x4553_2000_0000_0000,
                ftQueueID = queue.ftQueueId,
                ftQueueItemID = queueItem.ftQueueItemId,
                ftCashBoxIdentification = "cashBoxIdentification",

                ftQueueRow = 1,
                ftReceiptIdentification = "0#",
                ftReceiptMoment = DateTime.UtcNow,
                ftSignatures = new[] {
                    new SignatureItem
                    {
                        ftSignatureType = (SignatureType) 0x4553_2000_0000_0001,
                        ftSignatureFormat = SignatureFormat.QRCode,
                        Caption = "[www.fiskaltrust.es]",
                        Data = "https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=VATTEST&numserie=1%2fTEST&fecha=31-12-2019&importe=182800.00"
                    },
                    new SignatureItem
                    {
                        ftSignatureType = SignatureTypeES.Huella.As<SignatureType>(),
                        ftSignatureFormat = SignatureFormat.Text,
                        Caption = "Huella",
                        Data = "testHuella"
                    },
                    new SignatureItem
                    {
                        ftSignatureType = SignatureTypeES.NIF.As<SignatureType>(),
                        ftSignatureFormat = SignatureFormat.Text,
                        Caption = "NIF",
                        Data = "testNIF"
                    },
                }.ToList()
            };

            previousQueueItem.request = JsonSerializer.Serialize(previousReceiptRequest);
            previousQueueItem.response = JsonSerializer.Serialize(previousReceiptResponse);

            var queueES = new ftQueueES()
            {
                SSCDSignQueueItemId = previousQueueItem.ftQueueItemId
            };
            var signaturCreationUnitES = new ftSignaturCreationUnitES
            {

            };

            var masterDataConfiguration = _fixture.Create<MasterDataConfiguration>();
            masterDataConfiguration.Outlet.VatId = "VATTEST";

            var configMock = new Mock<IConfigurationRepository>();
            configMock.Setup(x => x.InsertOrUpdateQueueAsync(It.IsAny<ftQueue>())).Returns(Task.CompletedTask);
            var configurationRepositoryMock = new Mock<IConfigurationRepository>();
            configurationRepositoryMock.Setup(x => x.GetQueueESAsync(queue.ftQueueId)).ReturnsAsync(queueES);
            var queueItemRepositoryMock = new Mock<IQueueItemRepository>();
            queueItemRepositoryMock.Setup(x => x.GetAsync(previousQueueItem.ftQueueItemId)).ReturnsAsync(previousQueueItem);
            //var config = new VeriFactuSCUConfiguration();
            var sut = new ReceiptCommandProcessorES(Mock.Of<ILogger<ReceiptCommandProcessorES>>(), new(() => Task.FromResult(Mock.Of<IESSSCD>())), new(() => Task.FromResult(configurationRepositoryMock.Object)), new(() => Task.FromResult((IMiddlewareQueueItemRepository)queueItemRepositoryMock.Object)), new(() => Task.FromResult(Mock.Of<IMiddlewareJournalESRepository>())));

            var receiptRequest = new ReceiptRequest
            {
                ftCashBoxID = Guid.NewGuid(),
                ftReceiptCase = (ReceiptCase)(0x4553_2000_0000_0000 | (long)ReceiptCase.InitialOperationReceipt0x4001),
                cbReceiptMoment = new DateTime(2019, 12, 31),
                cbReceiptReference = "TEST",
                cbChargeItems = [
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0008,
                        Amount = 12000.00m,
                        VATAmount = 0m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0001,
                        Amount = 15900m,
                        VATAmount = 900m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0006,
                        Amount = 56500m,
                        VATAmount = 6500m,
                        Description = "Description",
                        Quantity = 1,
                        VATRate = 23m
                    },
                    new ChargeItem
                    {
                        ftChargeItemCase = (ChargeItemCase) 0x4553_2000_0000_0003,
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
                ftState = (State)0x4553_2000_0000_0000,
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
                ftSignatureType = (SignatureType)0x4553_2000_0000_0001,
                ftSignatureFormat = SignatureFormat.QRCode,
                Caption = "[www.fiskaltrust.es]",
                Data = "https://prewww2.aeat.es/wlpl/TIKE-CONT/ValidarQR?nif=VATTEST&numserie=1%2fTEST&fecha=31-12-2019&importe=182800.00"
            };
            result.receiptResponse.ftQueueID.Should().Be(receiptResponse.ftQueueID);
            result.receiptResponse.ftQueueItemID.Should().Be(receiptResponse.ftQueueItemID);
            result.receiptResponse.ftReceiptIdentification.Should().Be("0#1/TEST");
            result.receiptResponse.ftSignatures[0].Should().BeEquivalentTo(expectedSignaturItem);
        }

        [Fact]
        public async Task PointOfSaleReceipt0x0001Async_Should_Set_MiddlewareStateData_ES_LastReceipt()
        {
            // Arrange
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();
            var previousQueueItem = TestHelpers.CreateQueueItem();

            var previousReceiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001
            };
            var previousReceiptResponse = new ReceiptResponse
            {
                ftState = (State)0x4553_2000_0000_0000,
                ftQueueID = queue.ftQueueId,
                ftQueueItemID = previousQueueItem.ftQueueItemId,
                ftQueueRow = 1,
                ftReceiptIdentification = "prev#",
                ftReceiptMoment = DateTime.UtcNow
            };
            previousQueueItem.request = JsonSerializer.Serialize(previousReceiptRequest);
            previousQueueItem.response = JsonSerializer.Serialize(previousReceiptResponse);

            var queueES = new ftQueueES()
            {
                SSCDSignQueueItemId = previousQueueItem.ftQueueItemId
            };

            var configurationRepositoryMock = new Mock<IConfigurationRepository>();
            configurationRepositoryMock.Setup(x => x.GetQueueESAsync(queue.ftQueueId)).ReturnsAsync(queueES);
            configurationRepositoryMock.Setup(x => x.InsertOrUpdateQueueESAsync(It.IsAny<ftQueueES>())).Returns(Task.CompletedTask);

            var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>();
            queueItemRepositoryMock.Setup(x => x.GetAsync(previousQueueItem.ftQueueItemId)).ReturnsAsync(previousQueueItem);

            var journalESRepositoryMock = new Mock<IMiddlewareJournalESRepository>();
            journalESRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftJournalES>())).Returns(Task.CompletedTask);

            var essscdMock = new Mock<IESSSCD>();
            essscdMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
                .ReturnsAsync((ProcessRequest req) => new ProcessResponse
                {
                    ReceiptResponse = req.ReceiptResponse
                });

            var sut = new ReceiptCommandProcessorES(
                Mock.Of<ILogger<ReceiptCommandProcessorES>>(),
                new(() => Task.FromResult(essscdMock.Object)),
                new(() => Task.FromResult(configurationRepositoryMock.Object)),
                new(() => Task.FromResult(queueItemRepositoryMock.Object)),
                new(() => Task.FromResult(journalESRepositoryMock.Object))
            );

            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State)0x4553_2000_0000_0000,
                ftQueueID = queue.ftQueueId,
                ftQueueItemID = queueItem.ftQueueItemId,
                ftQueueRow = 2,
                ftReceiptIdentification = "current#",
                ftReceiptMoment = DateTime.UtcNow,
                ftStateData = new
                {
                    ExtraStuff = 2
                }
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(receiptResponse))!);

            // Act
            var result = await sut.PointOfSaleReceipt0x0001Async(request);

            // Assert
            result.receiptResponse.Should().BeEquivalentTo(receiptResponse, options => options.Excluding(x => x.ftStateData));
            result.actionJournals.Should().BeEmpty();
            result.receiptResponse.ftState.Should().Be(0x4553_2000_0000_0000);

            var stateData = result.receiptResponse.ftStateData as MiddlewareStateData;
            stateData!.ExtraData.Should().ContainKey("ExtraStuff");
            stateData.Should().NotBeNull();
            stateData!.ES.Should().NotBeNull();
            stateData.ES!.LastReceipt.Should().NotBeNull();
            stateData.ES!.LastReceipt!.Request.ftReceiptCase.Should().Be(ReceiptCase.PointOfSaleReceipt0x0001);
            stateData.ES!.LastReceipt!.Response.ftReceiptIdentification.Should().Be("prev#");
        }

        [Fact]
        public async Task PointOfSaleReceipt0x0001Async_Should_Handle_No_LastReceipt()
        {
            // Arrange
            var queue = TestHelpers.CreateQueue();
            var queueItem = TestHelpers.CreateQueueItem();

            var queueES = new ftQueueES()
            {
                SSCDSignQueueItemId = null
            };

            var configurationRepositoryMock = new Mock<IConfigurationRepository>();
            configurationRepositoryMock.Setup(x => x.GetQueueESAsync(queue.ftQueueId)).ReturnsAsync(queueES);
            configurationRepositoryMock.Setup(x => x.InsertOrUpdateQueueESAsync(It.IsAny<ftQueueES>())).Returns(Task.CompletedTask);

            var queueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>();

            var journalESRepositoryMock = new Mock<IMiddlewareJournalESRepository>();
            journalESRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<ftJournalES>())).Returns(Task.CompletedTask);

            var essscdMock = new Mock<IESSSCD>();
            essscdMock.Setup(x => x.ProcessReceiptAsync(It.IsAny<ProcessRequest>()))
                .ReturnsAsync((ProcessRequest req) => new ProcessResponse
                {
                    ReceiptResponse = req.ReceiptResponse
                });

            var sut = new ReceiptCommandProcessorES(
                Mock.Of<ILogger<ReceiptCommandProcessorES>>(),
                new(() => Task.FromResult(essscdMock.Object)),
                new(() => Task.FromResult(configurationRepositoryMock.Object)),
                new(() => Task.FromResult(queueItemRepositoryMock.Object)),
                new(() => Task.FromResult(journalESRepositoryMock.Object))
            );

            var receiptRequest = new ReceiptRequest
            {
                ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001
            };
            var receiptResponse = new ReceiptResponse
            {
                ftState = (State)0x4553_2000_0000_0000,
                ftQueueID = queue.ftQueueId,
                ftQueueItemID = queueItem.ftQueueItemId,
                ftQueueRow = 1,
                ftReceiptIdentification = "current#",
                ftReceiptMoment = DateTime.UtcNow,
                ftStateData = new
                {
                    ExtraStuff = 5
                }
            };

            var request = new ProcessCommandRequest(queue, receiptRequest, JsonSerializer.Deserialize<ReceiptResponse>(JsonSerializer.Serialize(receiptResponse))!);

            // Act
            var result = await sut.PointOfSaleReceipt0x0001Async(request);

            // Assert
            result.receiptResponse.Should().BeEquivalentTo(receiptResponse, options => options.Excluding(x => x.ftStateData));
            result.actionJournals.Should().BeEmpty();
            result.receiptResponse.ftState.Should().Be(0x4553_2000_0000_0000);

            var stateData = result.receiptResponse.ftStateData as MiddlewareStateData;
            stateData!.ExtraData.Should().ContainKey("ExtraStuff");
            stateData.Should().NotBeNull();
            stateData!.ES.Should().NotBeNull();
            stateData.ES!.LastReceipt.Should().BeNull();
        }

        [Fact]
        public void MiddlewareStateData_Should_SerializeAndDeserialize_ES()
        {
            var lastReceipt = new v2.Models.Receipt
            {
                Request = new ReceiptRequest { ftReceiptCase = ReceiptCase.PointOfSaleReceipt0x0001 },
                Response = new ReceiptResponse { ftState = (State)0x4553_2000_0000_0000 }
            };

            var governmentApi = new GovernmentAPI
            {
                Request = "request",
                Response = "response",
                Version = "v1"
            };

            var stateData = new MiddlewareStateData
            {
                ES = new MiddlewareStateDataES
                {
                    LastReceipt = lastReceipt,
                    GovernmentAPI = governmentApi,
                    SerieFactura = "I",
                    NumFactura = 1
                }
            };

            var json = JsonSerializer.Serialize(stateData);
            var deserialized = JsonSerializer.Deserialize<MiddlewareStateData>(json);

            deserialized.Should().NotBeNull();
            deserialized!.ES.Should().NotBeNull();
            deserialized.ES!.LastReceipt.Should().NotBeNull();
            deserialized.ES!.GovernmentAPI.Should().NotBeNull();
            deserialized.ES!.GovernmentAPI!.Request.Should().Be("request");
            deserialized.ES!.GovernmentAPI!.Response.Should().Be("response");
            deserialized.ES!.GovernmentAPI!.Version.Should().Be("v1");
        }
    }
}
