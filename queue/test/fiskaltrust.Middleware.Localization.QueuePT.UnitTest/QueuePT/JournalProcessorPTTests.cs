using System.Text;
using System.Text.Json;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.Models.Cases;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;
using fiskaltrust.Middleware.Contracts.Repositories;
using fiskaltrust.Middleware.Localization.v2.Helpers;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT;

public class JournalProcessorPTTests
{
    [Fact]
    public async Task JournalProcessorPT_ShouldReturnJournalResponse()
    {
        var storageProvider = new Mock<IStorageProvider>();
        var middlewareQueueItemRepositoryMock = new Mock<IMiddlewareQueueItemRepository>();
        var queueItems = new List<ftQueueItem>
        {
            new ftQueueItem
            {
                request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                response = JsonSerializer.Serialize(new ReceiptResponse
                {
                    ftState = (State) 0x5054_2000_0000_0000,
                    ftCashBoxIdentification = "cashBoxIdentification",
                    ftQueueID = Guid.NewGuid(),
                    ftQueueItemID = Guid.NewGuid(),
                    ftQueueRow = 1,
                    ftReceiptIdentification = "ft1234#FS 123893/2444",
                    ftSignatures = [
                        new SignatureItem {
                            Data = "hash_data",
                            ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
                            ftSignatureFormat = (SignatureFormat) 0x001,
                        },
                        new SignatureItem {
                            Data = "atcud_data",
                            ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
                            ftSignatureFormat = (SignatureFormat) 0x001,
                        }
                    ],
                    ftReceiptMoment = DateTime.UtcNow,
                }),
            },
            new ftQueueItem
            {
                request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                response = JsonSerializer.Serialize(new ReceiptResponse
                {
                    ftState = (State) 0x5054_2000_0000_0000,
                    ftCashBoxIdentification = "cashBoxIdentification",
                    ftQueueID = Guid.NewGuid(),
                    ftQueueItemID = Guid.NewGuid(),
                    ftQueueRow = 1,
                    ftReceiptIdentification = "ft1234#FS 123893/2444",
                    ftSignatures = [
                        new SignatureItem {
                            Data = "hash_data",
                            ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
                            ftSignatureFormat = (SignatureFormat) 0x001,
                        },
                        new SignatureItem {
                            Data = "atcud_data",
                            ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
                            ftSignatureFormat = (SignatureFormat) 0x001,
                        }
                    ],
                    ftReceiptMoment = DateTime.UtcNow,
                }),
            },
            new ftQueueItem
            {
                request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                response = JsonSerializer.Serialize(new ReceiptResponse
                {
                    ftState = (State) 0x5054_2000_0000_0000,
                    ftCashBoxIdentification = "cashBoxIdentification",
                    ftQueueID = Guid.NewGuid(),
                    ftQueueItemID = Guid.NewGuid(),
                    ftQueueRow = 1,
                    ftReceiptIdentification = "ft1234#FS 123893/2444",
                    ftReceiptMoment = DateTime.UtcNow,
                    ftSignatures = [
                        new SignatureItem {
                            Data = "hash_data",
                            ftSignatureType = SignatureTypePT.Hash.As<SignatureType>(),
                            ftSignatureFormat = (SignatureFormat) 0x001,
                        },
                        new SignatureItem {
                            Data = "atcud_data",
                            ftSignatureType = SignatureTypePT.ATCUD.As<SignatureType>(),
                            ftSignatureFormat = (SignatureFormat) 0x001,
                        }
                    ]
                }),
            }
        };
        middlewareQueueItemRepositoryMock.Setup(x => x.GetAsync()).ReturnsAsync(queueItems);
        storageProvider.Setup(x => x.CreateMiddlewareQueueItemRepository()).Returns(new AsyncLazy<IMiddlewareQueueItemRepository>(() => Task.FromResult(middlewareQueueItemRepositoryMock.Object)));
        var processor = new JournalProcessorPT(storageProvider.Object);
        var result = processor.ProcessAsync(new JournalRequest());
        var byteArrays = await result.result.ToArrayAsync();
        var flattenedBytes = byteArrays.SelectMany(x => x).ToArray();
        var data = (result.contentType.CharSet is null ? Encoding.Default : Encoding.GetEncoding(result.contentType.CharSet!)).GetString(flattenedBytes);
    }
}
