using System.Collections.Generic;
using fiskaltrust.ifPOS.v2;
using fiskaltrust.ifPOS.v2.Cases;
using fiskaltrust.Middleware.SCU.FR.InfoCert.Signing;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.FR.UnitTest.InfoCert;

/// <summary>
/// The signed receipt totals use the same buckets as the queue's period totals: this is the
/// queue's FRTotalsCalculatorTests table (QueueFR v1.2 as the oracle; card, crossed cheque,
/// voucher and Ticket Restaurant non-cash by decision), run against the InfoCert copy.
/// </summary>
public class InfoCertTotalsTests
{
    public static TheoryData<long, decimal, string> VatCases => new()
    {
        { 0x4652_2000_0000_0000, 5.5m, "CIReduced1" },
        { 0x4652_2000_0000_0000, 10m, "CIReduced2" },
        { 0x4652_2000_0000_0000, 20m, "CINormal" },
        { 0x4652_2000_0000_0000, 0m, "CIZero" },
        { 0x4652_2000_0000_0000, 2.1m, "CIReducedS" },
        { 0x4652_2000_0000_0001, 5.5m, "CIReduced1" },
        { 0x4652_2000_0000_0002, 10m, "CIReduced2" },
        { 0x4652_2000_0000_0003, 20m, "CINormal" },
        { 0x4652_2000_0000_0004, 2.1m, "CIReducedS" },
        { 0x4652_2000_0000_0005, 0m, "CIZero" },
        { 0x4652_2000_0000_0006, 0m, "CIZero" },
        { 0x4652_2000_0000_0007, 0m, "CIZero" },
        { 0x4652_2000_0000_0008, 0m, "CIZero" },
        { 0x4652_2000_0000_0007, 20m, "CIZero" },
        { 0x4652_2000_0000_0048, 0m, "CIZero" },
        { 0x4652_2000_0000_000F, 0m, "CIUnknown" },
    };

    public static TheoryData<long, string> PayCases => new()
    {
        { 0x4652_2000_0000_0000, "PICash" },
        { 0x4652_2000_0000_0001, "PICash" },
        { 0x4652_2000_0000_0002, "PINonCash" },
        { 0x4652_2000_0000_0003, "PINonCash" },
        { 0x4652_2000_0000_0004, "PINonCash" },
        { 0x4652_2000_0000_0005, "PINonCash" },
        { 0x4652_2000_0000_0006, "PINonCash" },
        { 0x4652_2000_0000_0007, "PINonCash" },
        { 0x4652_2000_0000_0008, "PINonCash" },
        { 0x4652_2000_0000_0009, "PIInternal" },
        { 0x4652_2000_0000_000A, "PINonCash" },
        { 0x4652_2000_0000_000B, "PINonCash" },
        { 0x4652_2000_0000_000C, "PIInternal" },
        { 0x4652_2000_0000_000D, "PIInternal" },
        { 0x4652_2000_0000_000E, "PIInternal" },
        { 0x4652_2000_0000_000F, "PINonCash" },
        { 0x4652_2000_0040_0001, "PIUnknown" },
        { 0x4652_2000_0000_00FF, "PIUnknown" },
    };

    [Theory]
    [MemberData(nameof(VatCases))]
    public void ChargeItem_LandsInTheQueuesBucket(long chargeItemCase, decimal vatRate, string bucket)
    {
        var totals = InfoCertTotals.From(new ReceiptRequest
        {
            cbChargeItems = [new ChargeItem { Amount = 10m, VATRate = vatRate, ftChargeItemCase = (ChargeItemCase) chargeItemCase }],
            cbPayItems = [],
        });

        Buckets(totals).Should().BeEquivalentTo(Expected(bucket, 10m));
        totals.Totalizer.Should().Be(10m);
    }

    [Theory]
    [MemberData(nameof(PayCases))]
    public void PayItem_LandsInTheQueuesBucket(long payItemCase, string bucket)
    {
        var totals = InfoCertTotals.From(new ReceiptRequest
        {
            cbChargeItems = [],
            cbPayItems = [new PayItem { Amount = -20m, ftPayItemCase = (PayItemCase) payItemCase }],
        });

        Buckets(totals).Should().BeEquivalentTo(Expected(bucket, -20m));
    }

    private static Dictionary<string, decimal> Buckets(InfoCertTotals t) => new()
    {
        ["CINormal"] = t.CINormal,
        ["CIReduced1"] = t.CIReduced1,
        ["CIReduced2"] = t.CIReduced2,
        ["CIReducedS"] = t.CIReducedS,
        ["CIZero"] = t.CIZero,
        ["CIUnknown"] = t.CIUnknown,
        ["PICash"] = t.PICash,
        ["PINonCash"] = t.PINonCash,
        ["PIInternal"] = t.PIInternal,
        ["PIUnknown"] = t.PIUnknown,
    };

    private static Dictionary<string, decimal> Expected(string bucket, decimal amount)
    {
        var expected = Buckets(default);
        expected[bucket] = amount;
        return expected;
    }
}
