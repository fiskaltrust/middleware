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
    /// Other actions that are not signed as Kassenbeleg-V1 (e.g. info-internal 0x0013 without payments -> AVSonstige,
    /// info-order 0x0010 without payments) export their charge items to Bonpos/Bonpos_USt.
    /// DSFinV-K 2.4 Tz. 3.1.2: the Bonkopf must be derivable from the positions, so Bonkopf and Bonkopf_USt hold the
    /// cumulated position values (AVSonstige used to be exported with Bonkopf 0.00 while Bonpos had amounts).
    /// </summary>
    public class OtherActionReconciliationTests
    {
        private const long ZNumber = 1185;
        private static readonly DateTime ClosingMoment = new(2026, 5, 2, 21, 19, 7, DateTimeKind.Utc);
        private static readonly DateTime ReceiptMoment = new(2026, 5, 2, 7, 58, 29, DateTimeKind.Utc);
        private static readonly DSFinVKParameters Parameters = new() { CashboxIdentification = "test-cbi" };

        public static TheoryData<long, string> OtherActionCases => new()
        {
            { 0x4445_0001_0000_0013, DSFinVKConstants.BonTypes.OtherActionElse },
            { 0x4445_0001_0000_0010, DSFinVKConstants.BonTypes.OtherActionElse },
        };

        [Theory]
        [MemberData(nameof(OtherActionCases))]
        public void Bonkopf_OtherActionWithChargeItems_HoldsCumulatedTurnover(long receiptCase, string bonType)
        {
            var entry = CreateEntry(receiptCase);
            entry.ReceiptRequest.IsReceiptProcessType().Should().BeFalse();

            var bonkopf = new BonkopfFactory(Parameters).Create(entry, ZNumber, ClosingMoment);

            bonkopf.BON_TYP.Should().Be(bonType);
            bonkopf.UMS_BRUTTO.Should().Be(71m);
        }

        [Theory]
        [MemberData(nameof(OtherActionCases))]
        public void BonkopfUst_OtherActionWithChargeItems_HoldsCumulatedAmountsPerVatKey(long receiptCase, string _)
        {
            var bonkopfUst = new BonkopfUstFactory(Parameters, Mock.Of<ILogger>()).Create(CreateEntry(receiptCase), ZNumber, ClosingMoment).ToList();

            bonkopfUst.Should().HaveCount(2);
            var general = bonkopfUst.Single(x => x.UST_SCHLUESSEL == DSFinVKConstants.VatKeys.General);
            general.BON_BRUTTO.Should().Be(71m);
            general.BON_UST.Should().BeApproximately(11.34m, 0.005m);
            (general.BON_NETTO + general.BON_UST).Should().Be(71m);
            bonkopfUst.Single(x => x.UST_SCHLUESSEL != DSFinVKConstants.VatKeys.General).BON_BRUTTO.Should().Be(0m);
        }

        // DSFinV-K 2.4 Tz. 3.1.2: the Bonkopf must be derivable from the positions.
        [Theory]
        [MemberData(nameof(OtherActionCases))]
        public void BonkopfAndBonpos_OtherActionWithChargeItems_Reconcile(long receiptCase, string _)
        {
            var logger = Mock.Of<ILogger>();
            var entry = CreateEntry(receiptCase);

            var bonkopf = new BonkopfFactory(Parameters).Create(entry, ZNumber, ClosingMoment);
            var bonpos = new BonposFactory(Parameters, logger).Create(entry, ZNumber, ClosingMoment).ToList();
            var bonposUst = new BonposUstFactory(Parameters, logger).Create(entry, ZNumber, ClosingMoment).ToList();
            var bonkopfUst = new BonkopfUstFactory(Parameters, logger).Create(entry, ZNumber, ClosingMoment).ToList();

            bonpos.Should().HaveCount(3);
            bonpos.Select(x => x.GV_TYP).Should().OnlyContain(x => x == DSFinVKConstants.BusinessCaseTypes.Revenue);
            bonposUst.Sum(x => x.POS_BRUTTO).Should().Be(bonkopf.UMS_BRUTTO);
            bonkopfUst.Sum(x => x.BON_BRUTTO).Should().Be(bonkopf.UMS_BRUTTO);
            foreach (var header in bonkopfUst)
            {
                var positions = bonposUst.Where(x => x.UST_SCHLUESSEL == header.UST_SCHLUESSEL).ToList();
                positions.Sum(x => x.POS_BRUTTO).Should().Be(header.BON_BRUTTO);
                positions.Sum(x => x.POS_NETTO).Should().Be(header.BON_NETTO);
                positions.Sum(x => x.POS_UST).Should().Be(header.BON_UST);
            }
        }

        // DSFinV-K 2.4 Anhang B: AV types (except AVTraining) only allow ZAHLART_TYP "Keine".
        [Theory]
        [MemberData(nameof(OtherActionCases))]
        public void BonkopfZahlart_OtherActionWithChargeItems_IsKeine(long receiptCase, string _)
        {
            var result = new BonkopfZahlartFactory(Parameters).Create(CreateEntry(receiptCase), ZNumber, ClosingMoment).ToList();

            result.Should().ContainSingle();
            result[0].ZAHLART_TYP.Should().Be("Keine");
            result[0].BASISWAEH_BETRAG.Should().Be(0m);
        }

        [Fact]
        public void BonkopfAndBonkopfUst_DailyClosingWithoutChargeItems_StayEmpty()
        {
            var entry = CreateEntry(0x4445_0001_0000_0007);
            entry.ReceiptRequest.cbChargeItems = Array.Empty<ChargeItem>();

            var bonkopf = new BonkopfFactory(Parameters).Create(entry, ZNumber, ClosingMoment);
            var bonpos = new BonposFactory(Parameters, Mock.Of<ILogger>()).Create(entry, ZNumber, ClosingMoment).ToList();
            var bonkopfUst = new BonkopfUstFactory(Parameters, Mock.Of<ILogger>()).Create(entry, ZNumber, ClosingMoment).ToList();

            bonkopf.UMS_BRUTTO.Should().Be(0m);
            bonpos.Should().BeEmpty();
            // Nothing to split by VAT, and UST_SCHLUESSEL 0 would reference a key missing in Stamm_USt (vat.csv)
            bonkopfUst.Should().BeEmpty();
        }

        private static ReceiptEntry CreateEntry(long receiptCase) => new()
        {
            QueueItem = new ftQueueItem(),
            ReceiptRequest = new ReceiptRequest
            {
                ftReceiptCase = receiptCase,
                cbReceiptMoment = ReceiptMoment,
                cbChargeItems = new[]
                {
                    // Basket as sent by the POS: services at 19% plus a 0.00 surcharge.
                    new ChargeItem { Position = 0, Quantity = 1, Description = "Haarschnitt", Amount = 55m, VATRate = 19, ftChargeItemCase = 0x4445_0000_0000_0011 },
                    new ChargeItem { Position = 1, Quantity = 1, Description = "Pflege", Amount = 16m, VATRate = 19, ftChargeItemCase = 0x4445_0000_0000_0011 },
                    new ChargeItem { Position = 2, Quantity = 1, Description = "Aufschlag", Amount = 0m, VATRate = 0, ftChargeItemCase = 0x4445_0000_0000_0018 }
                },
                cbPayItems = Array.Empty<PayItem>()
            },
            ReceiptResponse = new ReceiptResponse
            {
                ftReceiptMoment = ReceiptMoment,
                ftReceiptIdentification = "ft3#IT31",
                ftSignatures = Array.Empty<SignaturItem>()
            }
        };
    }
}
