using fiskaltrust.Middleware.SCU.PL.Abstraction;
using fiskaltrust.Middleware.SCU.PL.TestSupport.PosNetPrinter;
using fiskaltrust.Middleware.SCU.PL.TestSupport.Verification;
using FluentAssertions;
using Xunit;

namespace fiskaltrust.Middleware.SCU.PL.AcceptanceTest;

/// <summary>
/// The read-back the verification rests on: what the register reports about itself before and after
/// a receipt, over the SCU's own connection, held against what the receipt request implies. Without a
/// cassette this runs against the device model; recorded against the printer it is the evidence that
/// the register totalized what the SCU sent.
/// </summary>
public class PosNetDeviceProbeTests
{
    [Fact]
    public async Task CashSale_LeavesExactlyItsFootprintOnTheRegister()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);
        var request = PLReceiptExamples.CashSale();

        var before = await target.Probe.SnapshotAsync();
        var result = await target.Sut.ProcessReceiptAsync(request);
        var transaction = await target.Probe.ReadTransactionAsync();
        var after = await target.Probe.SnapshotAsync();

        // The probe's reads are on the wire but are not the SCU's commands.
        target.SentMnemonics.Should().Equal("scomm", "trinit", "trline", "trpayment", "trend", "scnt");
        var expected = FiscalFootprint.Of(request.ReceiptRequest, new PtuSlotResolver(target.Configuration.VatRateTable));
        FootprintComparer.Compare(expected, transaction, before, after, FiscalDocumentNumber.Of(result.ReceiptResponse)).Should().BeEmpty();
    }

    [Fact]
    public async Task StornoOfDiscountedSale_LeavesTheValueOfWhatIsLeft()
    {
        using var target = PosNetTestTarget.Open(TestProject.Cassettes);
        var request = PLReceiptExamples.StornoOfDiscountedSale();

        var before = await target.Probe.SnapshotAsync();
        var result = await target.Sut.ProcessReceiptAsync(request);
        var transaction = await target.Probe.ReadTransactionAsync();
        var after = await target.Probe.SnapshotAsync();

        // Kawa 10.00 less 2.00, Piwo 8.00, the coffee reversed: 8.00 in PTU B is what stays.
        var expected = FiscalFootprint.Of(request.ReceiptRequest, new PtuSlotResolver(target.Configuration.VatRateTable));
        expected.TotalGrosze.Should().Be(800);
        FootprintComparer.Compare(expected, transaction, before, after, FiscalDocumentNumber.Of(result.ReceiptResponse)).Should().BeEmpty();
    }
}
