﻿using System.Text;
using System.Text.Json;
using fiskaltrust.Api.POS.Models.ifPOS.v2;
using fiskaltrust.Middleware.Localization.QueuePT.Interface;
using fiskaltrust.Middleware.Localization.QueuePT.Processors;
using fiskaltrust.Middleware.Localization.v2.Interface;
using fiskaltrust.storage.V0;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Localization.QueuePT.UnitTest.QueuePT
{
    public class JournalProcessorPTTests
    {
        [Fact]
        public async Task JournalProcessorPT_ShouldReturnJournalResponse()
        {
            var storageProvider = new Mock<IStorageProvider>();
            var queueItems = new List<ftQueueItem>
            {
                new ftQueueItem
                {
                    request = JsonSerializer.Serialize(ReceiptExamples.CASH_SALES_RECEIPT),
                    response = JsonSerializer.Serialize(new ReceiptResponse
                    {
                        ftState = 0x5054_2000_0000_0000,
                        ftCashBoxIdentification = "cashBoxIdentification",
                        ftQueueID = Guid.NewGuid(),
                        ftQueueItemID = Guid.NewGuid(),
                        ftQueueRow = 1,
                        ftReceiptIdentification = "ft1234#FS 123893/2444",
                        ftSignatures = [
                            new SignatureItem {
                                Data = "hash_data",
                                ftSignatureType = (long) SignatureTypesPT.Hash,
                                ftSignatureFormat = 0x001,
                            },
                            new SignatureItem {
                                Data = "atcud_data",
                                ftSignatureType = (long) SignatureTypesPT.ATCUD,
                                ftSignatureFormat = 0x001,
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
                        ftState = 0x5054_2000_0000_0000,
                        ftCashBoxIdentification = "cashBoxIdentification",
                        ftQueueID = Guid.NewGuid(),
                        ftQueueItemID = Guid.NewGuid(),
                        ftQueueRow = 1,
                        ftReceiptIdentification = "ft1234#FS 123893/2444",
                        ftSignatures = [
                            new SignatureItem {
                                Data = "hash_data",
                                ftSignatureType = (long) SignatureTypesPT.Hash,
                                ftSignatureFormat = 0x001,
                            },
                            new SignatureItem {
                                Data = "atcud_data",
                                ftSignatureType = (long) SignatureTypesPT.ATCUD,
                                ftSignatureFormat = 0x001,
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
                        ftState = 0x5054_2000_0000_0000,
                        ftCashBoxIdentification = "cashBoxIdentification",
                        ftQueueID = Guid.NewGuid(),
                        ftQueueItemID = Guid.NewGuid(),
                        ftQueueRow = 1,
                        ftReceiptIdentification = "ft1234#FS 123893/2444",
                        ftReceiptMoment = DateTime.UtcNow,
                        ftSignatures = [
                            new SignatureItem {
                                Data = "hash_data",
                                ftSignatureType = (long) SignatureTypesPT.Hash,
                                ftSignatureFormat = 0x001,
                            },
                            new SignatureItem {
                                Data = "atcud_data",
                                ftSignatureType = (long) SignatureTypesPT.ATCUD,
                                ftSignatureFormat = 0x001,
                            }
                        ]
                    }),
                }
            };
            storageProvider.Setup(x => x.GetMiddlewareQueueItemRepository().GetAsync()).ReturnsAsync(queueItems);
            var processor = new JournalProcessorPT(storageProvider.Object);
            var result = processor.ProcessAsync(new ifPOS.v1.JournalRequest());
            var journalResponse = await result.ToListAsync();
            var data = Encoding.UTF8.GetString(journalResponse.SelectMany(x => x.Chunk).ToArray());
        }
    }
}