using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.MasterData
{
    public class TseTransaktionFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput_ForSingleSignatureBlock()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItem = GetTestDataWithSingleSignatureBlock();

            var sut = new TseTransaktionFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment).ToList();

            result.Should().HaveCount(1);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft1#IT1");
            result[0].TSE_ID.Should().Be(1);
            result[0].TSE_TANR.Should().Be(11);
            result[0].TSE_TA_START.Should().Be(new DateTime(2020, 09, 26, 23, 12, 11, 123, DateTimeKind.Utc));
            result[0].TSE_TA_ENDE.Should().Be(new DateTime(2020, 09, 26, 23, 12, 12, 123, DateTimeKind.Utc));
            result[0].TSE_TA_VORGANGSART.Should().Be("Kassenbeleg-V1");
            result[0].TSE_TA_SIGZ.Should().Be(123);
            result[0].TSE_TA_SIG.Should().Be("ZUGUziagsdadbkjahd897123==");
        }

        [Fact]
        public void Create_ShouldGenerateValidOutput_ForMultipleSignatureBlocks()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12);
            var queueItem = GetTestDataWithMultipleSignatureBlocks();

            var sut = new TseTransaktionFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment).ToList();

            result.Should().HaveCount(3);

            result[0].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[0].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[0].Z_NR.Should().Be(zNumber);
            result[0].BON_ID.Should().Be("ft1#IT1");
            result[0].TSE_ID.Should().Be(1);
            result[0].TSE_TANR.Should().Be(11);
            result[0].TSE_TA_START.Should().Be(new DateTime(2020, 09, 26, 23, 12, 11, 123, DateTimeKind.Utc));
            result[0].TSE_TA_ENDE.Should().Be(new DateTime(2020, 09, 26, 23, 12, 12, 123, DateTimeKind.Utc));
            result[0].TSE_TA_VORGANGSART.Should().Be("Kassenbeleg-V1");
            result[0].TSE_TA_SIGZ.Should().Be(123);
            result[0].TSE_TA_SIG.Should().Be("ZUGUziagsdadbkjahd897123==");

            result[1].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[1].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[1].Z_NR.Should().Be(zNumber);
            result[1].BON_ID.Should().Be("ft1#IT1");
            result[1].TSE_ID.Should().Be(1);
            result[1].TSE_TANR.Should().Be(12);
            result[1].TSE_TA_START.Should().Be(new DateTime(2020, 09, 26, 23, 13, 11, 123, DateTimeKind.Utc));
            result[1].TSE_TA_ENDE.Should().Be(new DateTime(2020, 09, 26, 23, 13, 12, 123, DateTimeKind.Utc));
            result[1].TSE_TA_VORGANGSART.Should().Be("Kassenbeleg-V1");
            result[1].TSE_TA_SIGZ.Should().Be(125);
            result[1].TSE_TA_SIG.Should().Be("YUGUziagsdadbkjahd897123==");

            result[2].Z_KASSE_ID.Should().Be(cashboxIdentification);
            result[2].Z_ERSTELLUNG.Should().Be(closingMoment);
            result[2].Z_NR.Should().Be(zNumber);
            result[2].BON_ID.Should().Be("ft1#IT1");
            result[2].TSE_ID.Should().Be(1);
            result[2].TSE_TANR.Should().Be(13);
            result[2].TSE_TA_START.Should().Be(new DateTime(2020, 09, 26, 23, 14, 11, 123, DateTimeKind.Utc));
            result[2].TSE_TA_ENDE.Should().Be(new DateTime(2020, 09, 26, 23, 14, 12, 123, DateTimeKind.Utc));
            result[2].TSE_TA_VORGANGSART.Should().Be("Kassenbeleg-V1");
            result[2].TSE_TA_SIGZ.Should().Be(127);
            result[2].TSE_TA_SIG.Should().Be("XUGUziagsdadbkjahd897123==");
        }

        private ReceiptEntry GetTestDataWithSingleSignatureBlock()
        {
            return new ReceiptEntry()
            {
                QueueItem =new ftQueueItem(),
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4445000000000001
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft1#IT1",
                    ftSignatures = new[]
                    {
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.ProcessType,
                            Caption = "<processType>",
                            Data = "Kassenbeleg-V1"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.TransactionNumber,
                            Caption = "<transaktions-nummer>",
                            Data = "11"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.SignatureCounter,
                            Caption = "<signatur-zaehler>",
                            Data = "123"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.StartTime,
                            Caption = "<start-zeit>",
                            Data = "2020-09-26T23:12:11.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.LogTime,
                            Caption = "<log-time>",
                            Data = "2020-09-26T23:12:12.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.Signature,
                            Caption = "<signatur>",
                            Data = "ZUGUziagsdadbkjahd897123=="
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.PublicKey,
                            Caption = "<public-key>",
                            Data = "BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA=="
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.ProcessData,
                            Caption = "<processData>",
                            Data = "Beleg^0.00_0.00_0.00_0.00_200.00^200.00:Bar"
                        }
                    }
                }
            };
        }

        private ReceiptEntry GetTestDataWithMultipleSignatureBlocks()
        {
            return new ReceiptEntry
            {
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4445000000000001
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptIdentification = "ft1#IT1",
                    ftSignatures = new[]
                    {
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.ProcessType,
                            Caption = "<processType>",
                            Data = "Kassenbeleg-V1"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.TransactionNumber,
                            Caption = "<transaktions-nummer>",
                            Data = "11"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.SignatureCounter,
                            Caption = "<signatur-zaehler>",
                            Data = "123"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.StartTime,
                            Caption = "<start-zeit>",
                            Data = "2020-09-26T23:12:11.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.LogTime,
                            Caption = "<log-time>",
                            Data = "2020-09-26T23:12:12.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.Signature,
                            Caption = "<signatur>",
                            Data = "ZUGUziagsdadbkjahd897123=="
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.PublicKey,
                            Caption = "<public-key>",
                            Data = "BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA=="
                        },

                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.ProcessType,
                            Caption = "<processType>",
                            Data = "Kassenbeleg-V1"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.TransactionNumber,
                            Caption = "<transaktions-nummer>",
                            Data = "12"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.SignatureCounter,
                            Caption = "<signatur-zaehler>",
                            Data = "125"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.StartTime,
                            Caption = "<start-zeit>",
                            Data = "2020-09-26T23:13:11.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.LogTime,
                            Caption = "<log-time>",
                            Data = "2020-09-26T23:13:12.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.Signature,
                            Caption = "<signatur>",
                            Data = "YUGUziagsdadbkjahd897123=="
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.PublicKey,
                            Caption = "<public-key>",
                            Data = "BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA=="
                        },

                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.ProcessType,
                            Caption = "<processType>",
                            Data = "Kassenbeleg-V1"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.TransactionNumber,
                            Caption = "<transaktions-nummer>",
                            Data = "13"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.SignatureCounter,
                            Caption = "<signatur-zaehler>",
                            Data = "127"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.StartTime,
                            Caption = "<start-zeit>",
                            Data = "2020-09-26T23:14:11.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.LogTime,
                            Caption = "<log-time>",
                            Data = "2020-09-26T23:14:12.123Z"
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.Signature,
                            Caption = "<signatur>",
                            Data = "XUGUziagsdadbkjahd897123=="
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.PublicKey,
                            Caption = "<public-key>",
                            Data = "BDtPHNfQi9mFA59OVTEg38uM0l8nfL45j2MT7TbPnnEInK+Redm8Jm0xLi3UooIy/FwExUjNibb43JG8e91osusawLqCUQBmkAfyIWuNQBMbj2pwZ20KAlAbOXM/1IMzbA=="
                        },
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.ProcessData,
                            Caption = "<processData>",
                            Data = "Beleg^0.00_0.00_0.00_0.00_200.00^200.00:Bar"
                        }
                    }
                }
            };
        }
    }
}
