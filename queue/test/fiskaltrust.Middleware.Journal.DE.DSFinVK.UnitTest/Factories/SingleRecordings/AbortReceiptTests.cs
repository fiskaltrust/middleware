using System;
using System.Linq;
using fiskaltrust.Middleware.Journal.DE.Common.Models;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Constants;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Extensions;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Factories.SingleRecordings;
using fiskaltrust.Middleware.Journal.DE.DSFinVK.Models;
using fiskaltrust.ifPOS.v1;
using fiskaltrust.storage.V0;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fiskaltrust.Middleware.Journal.DE.DSFinVK.UnitTest.Factories.SingleRecordings
{
    /// <summary>
    /// AbortReceipt (v0 0x4445_0001_0000_001A) is exported as BON_TYP "AVBelegabbruch".
    /// The request below is the v2 example receipt (0x4445_2040_0000_3004, charge items 0x4445_2000_0000_0013)
    /// as mapped by DEMapping in service-possystem-api: receipt case -> 0x4445_0001_0000_001A,
    /// charge item case -> 0x4445_0000_0000_0011 (delivery, normal VAT), VATRate 19 unchanged.
    /// </summary>
    public class AbortReceiptTests
    {
        private const string CashboxIdentification = "test-cbi";
        private const long ZNumber = 5;
        private static readonly DateTime ClosingMoment = new(2026, 9, 25, 20, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ReceiptMoment = new(2026, 9, 25, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DSFinVKParameters Parameters = new() { CashboxIdentification = CashboxIdentification };

        [Fact]
        public void GetBonType_MappedAbortReceipt_IsAVBelegabbruch()
        {
            var request = CreateMappedAbortRequest();

            request.GetBonType().Should().Be(DSFinVKConstants.BonTypes.OtherActionFailed);
            request.GetBonType().Should().Be("AVBelegabbruch");
            request.GetTseProcessType().Should().Be(DSFinVKConstants.ProcessTypes.KassenbelegV1);
        }

        [Fact]
        public void Bonkopf_MappedAbortReceipt_HasAbortTypeAndNoStorno()
        {
            var result = new BonkopfFactory(Parameters).Create(CreateEntry(), ZNumber, ClosingMoment);

            result.Z_KASSE_ID.Should().Be(CashboxIdentification);
            result.Z_NR.Should().Be(ZNumber);
            result.BON_ID.Should().Be("ft3#IT31");
            result.BON_TYP.Should().Be("AVBelegabbruch");
            result.BON_NAME.Should().BeNull();
            result.BON_STORNO.Should().BeFalse();
            result.BON_START.Should().Be(ReceiptMoment);
            result.BON_ENDE.Should().Be(ReceiptMoment);
            result.BEDIENER_ID.Should().Be("Nicht angegeben");
        }

        // DSFinV-K 2.4 Anhang I: "AVBelegabbruch^0.00_0.00_0.00_0.00_0.00^" - an aborted transaction has no turnover.
        [Fact]
        public void Bonkopf_MappedAbortReceipt_HasZeroTurnover()
        {
            var result = new BonkopfFactory(Parameters).Create(CreateEntry(), ZNumber, ClosingMoment);

            result.UMS_BRUTTO.Should().Be(0m);
        }

        // DSFinV-K 2.4 Tz. 4.2.3 / Anhang I: StartTransaction signs empty processData, so the positions of an aborted transaction were
        // never signed and are cancelled during entry: P_STORNO = 1 on the original position, no second line.
        [Fact]
        public void Bonpos_MappedAbortReceipt_HasCancelledRevenueLinePerChargeItem()
        {
            var sut = new BonposFactory(Parameters, Mock.Of<ILogger>());

            var result = sut.Create(CreateEntry(), ZNumber, ClosingMoment).ToList();

            result.Should().HaveCount(2);
            result.Select(x => x.POS_ZEILE).Should().Equal("0", "1");
            result.Select(x => x.GV_TYP).Should().OnlyContain(x => x == DSFinVKConstants.BusinessCaseTypes.Revenue);
            result.Select(x => x.P_STORNO).Should().OnlyContain(x => x == true);
            result.Select(x => x.ARTIKELTEXT).Should().Equal("Beer 0.3l", "Coffee");
            result.Select(x => x.STK_BR).Should().Equal(4.6m, 3.6m);
            result.Select(x => x.MENGE).Should().Equal(1m, 1m);
        }

        [Fact]
        public void BonposUstAndBonkopfUst_MappedAbortReceipt_UseGeneralVatKey()
        {
            var logger = Mock.Of<ILogger>();

            var bonposUst = new BonposUstFactory(Parameters, logger).Create(CreateEntry(), ZNumber, ClosingMoment).ToList();
            var bonkopfUst = new BonkopfUstFactory(Parameters, logger).Create(CreateEntry(), ZNumber, ClosingMoment).ToList();

            // v0 0x0011 (delivery, normal VAT) + VATRate 19 -> UST_SCHLUESSEL 1 (19%), not the 10.7% average rate.
            // The cancelled positions keep their amounts (Tz. 4.2.3: no second line).
            bonposUst.Should().HaveCount(2);
            bonposUst.Select(x => x.UST_SCHLUESSEL).Should().OnlyContain(x => x == DSFinVKConstants.VatKeys.General);
            bonposUst.Select(x => x.POS_BRUTTO).Should().Equal(4.6m, 3.6m);

            bonkopfUst.Should().ContainSingle();
            bonkopfUst[0].UST_SCHLUESSEL.Should().Be(DSFinVKConstants.VatKeys.General);
            bonkopfUst[0].BON_BRUTTO.Should().Be(0m);
            bonkopfUst[0].BON_NETTO.Should().Be(0m);
            bonkopfUst[0].BON_UST.Should().Be(0m);
        }

        // DSFinV-K 2.4 Tz. 3.1.2: the Bonkopf must be derivable from the positions that are not cancelled (P_STORNO = 0).
        [Fact]
        public void BonkopfAndBonpos_MappedAbortReceipt_ReconcileWithoutCancelledPositions()
        {
            var logger = Mock.Of<ILogger>();

            var bonkopf = new BonkopfFactory(Parameters).Create(CreateEntry(), ZNumber, ClosingMoment);
            var bonpos = new BonposFactory(Parameters, logger).Create(CreateEntry(), ZNumber, ClosingMoment).ToList();
            var bonposUst = new BonposUstFactory(Parameters, logger).Create(CreateEntry(), ZNumber, ClosingMoment).ToList();
            var bonkopfUst = new BonkopfUstFactory(Parameters, logger).Create(CreateEntry(), ZNumber, ClosingMoment).ToList();

            var cancelledLines = bonpos.Where(x => x.P_STORNO).Select(x => x.POS_ZEILE).ToHashSet();
            var activePositions = bonposUst.Where(x => !cancelledLines.Contains(x.POS_ZEILE)).ToList();

            activePositions.Sum(x => x.POS_BRUTTO).Should().Be(bonkopf.UMS_BRUTTO);
            foreach (var header in bonkopfUst)
            {
                var positions = activePositions.Where(x => x.UST_SCHLUESSEL == header.UST_SCHLUESSEL).ToList();
                positions.Sum(x => x.POS_BRUTTO).Should().Be(header.BON_BRUTTO);
                positions.Sum(x => x.POS_NETTO).Should().Be(header.BON_NETTO);
                positions.Sum(x => x.POS_UST).Should().Be(header.BON_UST);
            }
        }

        [Fact]
        public void BonkopfAndBonpos_PosReceipt_AreNotCancelled()
        {
            var logger = Mock.Of<ILogger>();
            var entry = CreateEntry();
            entry.ReceiptRequest.ftReceiptCase = 0x4445_0001_0000_0001;

            var bonkopf = new BonkopfFactory(Parameters).Create(entry, ZNumber, ClosingMoment);
            var bonpos = new BonposFactory(Parameters, logger).Create(entry, ZNumber, ClosingMoment).ToList();
            var bonposUst = new BonposUstFactory(Parameters, logger).Create(entry, ZNumber, ClosingMoment).ToList();
            var bonkopfUst = new BonkopfUstFactory(Parameters, logger).Create(entry, ZNumber, ClosingMoment).ToList();

            bonkopf.BON_TYP.Should().Be(DSFinVKConstants.BonTypes.Receipt);
            bonkopf.UMS_BRUTTO.Should().Be(8.2m);
            bonpos.Select(x => x.MENGE).Should().Equal(1m, 1m);
            bonpos.Select(x => x.P_STORNO).Should().OnlyContain(x => x == false);
            bonposUst.Select(x => x.POS_BRUTTO).Should().Equal(4.6m, 3.6m);
            bonkopfUst.Should().ContainSingle().Which.BON_BRUTTO.Should().Be(8.2m);
        }

        // DSFinV-K 2.4 Anhang B: AV types (except AVTraining) only allow ZAHLART_TYP "Keine".
        [Fact]
        public void BonkopfZahlart_MappedAbortReceipt_IsKeine()
        {
            var result = new BonkopfZahlartFactory(Parameters).Create(CreateEntry(), ZNumber, ClosingMoment).ToList();

            result.Should().ContainSingle();
            result[0].ZAHLART_TYP.Should().Be("Keine");
        }

        private static ReceiptEntry CreateEntry() => new()
        {
            QueueItem = new ftQueueItem { cbReceiptReference = "2026-09-25-fail-transaction-receipt" },
            ReceiptRequest = CreateMappedAbortRequest(),
            ReceiptResponse = new ReceiptResponse
            {
                ftReceiptMoment = ReceiptMoment,
                ftReceiptIdentification = "ft3#IT31",
                ftSignatures = Array.Empty<SignaturItem>()
            }
        };

        private static ReceiptRequest CreateMappedAbortRequest() => new()
        {
            ftReceiptCase = 0x4445_0001_0000_001A,
            cbReceiptReference = "2026-09-25-fail-transaction-receipt",
            cbReceiptMoment = ReceiptMoment,
            cbChargeItems = new[]
            {
                new ChargeItem { Position = 1, Quantity = 1, Description = "Beer 0.3l", Amount = 4.6m, VATRate = 19, ftChargeItemCase = 0x4445_0000_0000_0011 },
                new ChargeItem { Position = 2, Quantity = 1, Description = "Coffee", Amount = 3.6m, VATRate = 19, ftChargeItemCase = 0x4445_0000_0000_0011 }
            },
            cbPayItems = Array.Empty<PayItem>()
        };
    }
}
