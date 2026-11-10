using System;
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
    public class BonkopfFactoryTests
    {
        [Fact]
        public void Create_ShouldGenerateValidOutput()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2020, 9, 25, 9, 17, 12, DateTimeKind.Utc);

            var processStartTime = new DateTime(2020, 1, 30, 12, 0, 0, DateTimeKind.Utc);
            var cbReceiptMoment = new DateTime(2020, 9, 25, 12, 23, 0, DateTimeKind.Utc);
            var queueItems = GetTestData(processStartTime, cbReceiptMoment);

            var sut = new BonkopfFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItems, zNumber, closingMoment);

            result.Z_KASSE_ID.Should().Be(cashboxIdentification);
            result.Z_ERSTELLUNG.Should().Be(closingMoment);
            result.Z_NR.Should().Be(zNumber);
            result.BON_ID.Should().Be("ft3#IT31");
            result.BON_NR.Should().Be(3);
            result.BON_TYP.Should().Be(DSFinVKConstants.BonTypes.Receipt);
            result.BON_NAME.Should().Be("Other Stuff");
            result.TERMINAL_ID.Should().Be(null);
            result.BON_STORNO.Should().Be(false);
            result.BON_START.Should().Be(processStartTime);
            result.BON_ENDE.Should().Be(cbReceiptMoment);
            result.BEDIENER_ID.Should().Be("Alf1");
            result.BEDIENER_NAME.Should().Be("Alf");
            result.KUNDE_NAME.Should().Be(null);
            result.KUNDE_ID.Should().Be(null);
            result.KUNDE_TYP.Should().Be(null);
            result.KUNDE_STRASSE.Should().Be(null);
            result.KUNDE_PLZ.Should().Be(null);
            result.KUNDE_ORT.Should().Be(null);
            result.KUNDE_LAND.Should().Be(null);
            result.KUNDE_USTID.Should().Be(null);
            result.BON_NOTIZ.Should().Be(null);
            result.UMS_BRUTTO.Should().Be(-140);
        }

        [Fact]
        public void PayItemMustBeTransformedToBusinessCase_NoVoidReceipts_ReviseAmount()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 88;
            var closingMoment = new DateTime(2021, 11, 9, 13, 10, 10, DateTimeKind.Utc);
            var receiptMoment = new DateTime(2021, 11, 9, 13, 11, 10, DateTimeKind.Utc);
            var processStart = new DateTime(2021, 11, 9, 13, 10, 10, DateTimeKind.Utc);
            var queueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4445000100000001,
                    cbReceiptMoment = receiptMoment,
                    cbPayItems = new[]
                    {
                        new PayItem
                        {
                            //Voucher not taxable
                            ftPayItemCase = 0x444500000000000D,
                            Quantity = 1,
                            Amount = 8369.45m
                        }
                    }
                },
                ReceiptResponse = GetResponseForReceiptMoment(receiptMoment, processStart)
            };
            var sut = new BonkopfFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment);
            result.UMS_BRUTTO.Should().Be(-8369.45m);
        }

        [Fact]
        public void CalculateBonStart_WithProcessStart_UseProcessStart()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2021, 10, 18, 13, 18, 10, DateTimeKind.Utc);
            var receiptMoment = new DateTime(2021, 10, 18, 10, 18, 10, DateTimeKind.Utc);
            var processStart = new DateTime(2021, 10, 18, 9, 18, 10, DateTimeKind.Utc);
            var queueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4445000100000001,
                    cbReceiptMoment = receiptMoment,
                },
                ReceiptResponse = GetResponseForReceiptMoment(receiptMoment, processStart)
            };
            var sut = new BonkopfFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment);

            result.BON_START.Should().Be(processStart);
        }

        [Fact]
        public void CalculateBonStart_NoChargeOrPayItem_UseReceiptMoment()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2021, 10, 18, 13, 18, 10, DateTimeKind.Utc);
            var receiptMoment = new DateTime(2021, 10, 18, 10, 18, 10, DateTimeKind.Utc);
            var queueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4445000100000001,
                    cbReceiptMoment = receiptMoment,
                },
                ReceiptResponse = GetResponseForReceiptMoment(receiptMoment, null)
            };
            var sut = new BonkopfFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment);

            result.BON_START.Should().Be(receiptMoment);
        }
        [Fact]
        public void CalculateBonStart_MinPayItemMoment_UsePayItemMoment()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2021, 10, 18, 13, 18, 10, DateTimeKind.Utc);
            var receiptMoment = new DateTime(2021, 10, 18, 11, 18, 10, DateTimeKind.Utc);
            var payItemMoment = new DateTime(2021, 10, 18, 10, 18, 1, DateTimeKind.Utc);
            var chargeItemMoment = new DateTime(2021, 10, 18, 12, 18, 10, DateTimeKind.Utc);
            var queueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = GetRequestForReceiptMoment(receiptMoment, payItemMoment, chargeItemMoment),
                ReceiptResponse = GetResponseForReceiptMoment(receiptMoment, null)
            };
            var sut = new BonkopfFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment);

            result.BON_START.Should().Be(payItemMoment);
        }
        [Fact]
        public void CalculateBonStart_MinChargeItemMoment_UseChargeItemMoment()
        {
            var cashboxIdentification = "test-cbi";
            var zNumber = 77;
            var closingMoment = new DateTime(2021, 10, 18, 13, 18, 10, DateTimeKind.Utc);
            var receiptMoment = new DateTime(2021, 10, 18, 11, 18, 10, DateTimeKind.Utc);
            var payItemMoment = new DateTime(2021, 10, 18, 12, 18, 10, DateTimeKind.Utc);
            var chargeItemMoment = new DateTime(2021, 10, 18, 10, 18, 10, DateTimeKind.Utc);
            var queueItem = new ReceiptEntry()
            {
                QueueItem = new ftQueueItem(),
                ReceiptRequest = GetRequestForReceiptMoment(receiptMoment, payItemMoment, chargeItemMoment),
                ReceiptResponse = GetResponseForReceiptMoment(receiptMoment, null)
            };
            var sut = new BonkopfFactory(new DSFinVKParameters { CashboxIdentification = cashboxIdentification });
            var result = sut.Create(queueItem, zNumber, closingMoment);
            result.BON_START.Should().Be(chargeItemMoment);
        }
        private ReceiptRequest GetRequestForReceiptMoment(DateTime receiptMoment, DateTime payItemMoment, DateTime chargeItemMoment)
        {
            return new ReceiptRequest
            {
                ftReceiptCase = 0x4445000100000001,
                cbReceiptMoment = receiptMoment,
                cbPayItems = new[]
                {
                    new PayItem
                    {
                        Moment = payItemMoment
                    }
                },
                cbChargeItems = new[]
                {
                    new ChargeItem
                    {
                        Moment = chargeItemMoment
                    }
                 }
            };
        }
        private ReceiptResponse GetResponseForReceiptMoment(DateTime receiptMoment, DateTime? vorgangsBeginn)
        {
            return new ReceiptResponse
            {
                ftReceiptMoment = receiptMoment,
                ftReceiptIdentification = "ft3#IT31",
                ftSignatures = vorgangsBeginn.HasValue ? new[]
                {
                    new SignaturItem
                    {
                        ftSignatureType = (long) SignatureTypesDE.VorgangsBeginn,
                        Data = vorgangsBeginn.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                    }
                 } : new SignaturItem[0]
            };
        }

        private ReceiptEntry GetTestData(DateTime processStartTime, DateTime cbReceiptMoment)
        {
            return new ReceiptEntry() { 
                QueueItem = new ftQueueItem
                {
                    cbReceiptReference = "REF1"
                },
                ReceiptRequest = new ReceiptRequest
                {
                    ftReceiptCase = 0x4445000000000001,
                    cbReceiptMoment = cbReceiptMoment,
                    cbTerminalID = "8-Screen-Terminal 3000",
                    cbUser = "Alf",
                    ftReceiptCaseData = "{ \"ReceiptName\":\"Other Stuff\", \"UserId\":\"Alf1\"}",
                    cbPayItems = new[]
                    {
                        new PayItem
                        {
                            Moment = cbReceiptMoment,
                            ftPayItemCase = 0x444500000000000D,
                            Amount = 150.0M
                        },
                        new PayItem
                        {
                            Moment = cbReceiptMoment,
                            ftPayItemCase = 0x4445000000000001,
                            Amount = -140.0M
                        }
                    },
                    cbChargeItems = new[]
                    {
                        new ChargeItem
                        {
                            Amount = 10
                        }
                    }
                },
                ReceiptResponse = new ReceiptResponse
                {
                    ftReceiptMoment = cbReceiptMoment,
                    ftReceiptIdentification = "ft3#IT31",
                    ftSignatures = new[]
                    {
                        new SignaturItem
                        {
                            ftSignatureType = (long) SignatureTypesDE.VorgangsBeginn,
                            Data = processStartTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                        }
                    }
                }
            };
        }
    }
}
